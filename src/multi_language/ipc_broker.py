"""
Inter-Process Communication Broker with Pub/Sub Pattern
Enables real-time message passing between services
"""

import json
import redis
import logging
from typing import Dict, List, Callable, Any, Optional
from dataclasses import dataclass, asdict
from datetime import datetime
import uuid
import asyncio
from threading import Thread

logger = logging.getLogger(__name__)


@dataclass
class Message:
    """Message definition"""
    id: str
    source_service: str
    topic: str
    payload: Dict[str, Any]
    timestamp: str
    priority: int = 5
    headers: Dict[str, str] = None

    def to_json(self) -> str:
        """Convert to JSON"""
        data = asdict(self)
        data['headers'] = self.headers or {}
        return json.dumps(data)

    @staticmethod
    def from_json(json_str: str) -> 'Message':
        """Create from JSON"""
        data = json.loads(json_str)
        return Message(**data)


class Topic:
    """Topic definition"""

    def __init__(self, name: str, description: str = "", 
                 message_retention: int = 3600):
        self.name = name
        self.description = description
        self.message_retention = message_retention  # seconds
        self.subscribers: List[str] = []

    def to_dict(self) -> Dict:
        """Convert to dictionary"""
        return {
            'name': self.name,
            'description': self.description,
            'message_retention': self.message_retention,
            'subscribers': len(self.subscribers),
        }


class IPCBroker:
    """Inter-Process Communication Broker using Redis"""

    def __init__(self, redis_host: str = 'localhost', redis_port: int = 6379):
        self.redis = redis.Redis(
            host=redis_host,
            port=redis_port,
            decode_responses=True
        )
        self.prefix = "aimran:broker:"
        self.topics: Dict[str, Topic] = {}
        self.subscribers: Dict[str, List[Callable]] = {}
        self.message_queue_size = 1000

    def create_topic(self, topic_name: str, description: str = "", 
                     message_retention: int = 3600) -> bool:
        """Create a new topic"""
        try:
            topic = Topic(topic_name, description, message_retention)
            self.topics[topic_name] = topic

            # Store in Redis
            self.redis.hset(
                f"{self.prefix}topics",
                topic_name,
                json.dumps(topic.to_dict())
            )

            logger.info(f"✅ Created topic: {topic_name}")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to create topic: {str(e)}")
            return False

    def publish(self, topic_name: str, payload: Dict[str, Any], 
               source_service: str = "system", priority: int = 5) -> bool:
        """Publish message to topic"""
        try:
            message = Message(
                id=str(uuid.uuid4()),
                source_service=source_service,
                topic=topic_name,
                payload=payload,
                timestamp=datetime.now().isoformat(),
                priority=priority
            )

            # Publish via Redis pub/sub
            self.redis.publish(
                f"{self.prefix}topic:{topic_name}",
                message.to_json()
            )

            # Store in message queue for persistence
            queue_key = f"{self.prefix}queue:{topic_name}"
            self.redis.lpush(queue_key, message.to_json())
            self.redis.ltrim(queue_key, 0, self.message_queue_size - 1)

            # Set TTL
            if topic_name in self.topics:
                ttl = self.topics[topic_name].message_retention
                self.redis.expire(queue_key, ttl)

            logger.info(f"📤 Published to {topic_name}: {payload}")
            return True
        except Exception as e:
            logger.error(f"❌ Failed to publish: {str(e)}")
            return False

    def subscribe(self, topic_name: str, callback: Callable) -> str:
        """Subscribe to topic"""
        try:
            subscription_id = str(uuid.uuid4())

            if topic_name not in self.subscribers:
                self.subscribers[topic_name] = []

            self.subscribers[topic_name].append(callback)

            # Start subscription thread
            thread = Thread(
                target=self._listen_to_topic,
                args=(topic_name, callback),
                daemon=True
            )
            thread.start()

            logger.info(f"📥 Subscribed to {topic_name} with ID: {subscription_id}")
            return subscription_id
        except Exception as e:
            logger.error(f"❌ Failed to subscribe: {str(e)}")
            return ""

    def _listen_to_topic(self, topic_name: str, callback: Callable):
        """Listen to topic messages"""
        try:
            pubsub = self.redis.pubsub()
            pubsub.subscribe(f"{self.prefix}topic:{topic_name}")

            for message in pubsub.listen():
                if message['type'] == 'message':
                    try:
                        msg = Message.from_json(message['data'])
                        callback(msg)
                    except Exception as e:
                        logger.error(f"❌ Error processing message: {str(e)}")
        except Exception as e:
            logger.error(f"❌ Listen failed: {str(e)}")

    def unsubscribe(self, topic_name: str, callback: Callable) -> bool:
        """Unsubscribe from topic"""
        try:
            if topic_name in self.subscribers:
                if callback in self.subscribers[topic_name]:
                    self.subscribers[topic_name].remove(callback)
                    logger.info(f"✅ Unsubscribed from {topic_name}")
                    return True
            return False
        except Exception as e:
            logger.error(f"❌ Failed to unsubscribe: {str(e)}")
            return False

    def get_message_history(self, topic_name: str, limit: int = 100) -> List[Message]:
        """Get message history for topic"""
        try:
            queue_key = f"{self.prefix}queue:{topic_name}"
            messages = self.redis.lrange(queue_key, 0, limit - 1)

            result = []
            for msg_json in messages:
                try:
                    msg = Message.from_json(msg_json)
                    result.append(msg)
                except:
                    pass

            return result
        except Exception as e:
            logger.error(f"❌ Failed to get history: {str(e)}")
            return []

    def list_topics(self) -> List[Dict]:
        """List all available topics"""
        try:
            topics_data = self.redis.hgetall(f"{self.prefix}topics")
            result = []
            for topic_name, topic_json in topics_data.items():
                try:
                    topic_data = json.loads(topic_json)
                    result.append(topic_data)
                except:
                    pass
            return result
        except Exception as e:
            logger.error(f"❌ Failed to list topics: {str(e)}")
            return []

    def get_topic_stats(self, topic_name: str) -> Dict[str, Any]:
        """Get statistics for topic"""
        try:
            queue_key = f"{self.prefix}queue:{topic_name}"

            # Get message count
            msg_count = self.redis.llen(queue_key)

            # Get subscribers count
            sub_count = len(self.subscribers.get(topic_name, []))

            return {
                'topic': topic_name,
                'message_count': msg_count,
                'subscribers': sub_count,
                'timestamp': datetime.now().isoformat(),
            }
        except Exception as e:
            logger.error(f"❌ Failed to get stats: {str(e)}")
            return {}


class EventBus:
    """Publish-Subscribe event bus for system events"""

    def __init__(self, broker: IPCBroker):
        self.broker = broker
        self.event_handlers: Dict[str, List[Callable]] = {}

        # Create system event topics
        self._initialize_system_topics()

    def _initialize_system_topics(self):
        """Initialize system event topics"""
        system_events = [
            'service.registered',
            'service.deregistered',
            'service.started',
            'service.stopped',
            'service.error',
            'health.check',
            'metrics.updated',
        ]

        for event_type in system_events:
            self.broker.create_topic(event_type)

    def emit(self, event_type: str, data: Dict[str, Any], 
            source: str = "system") -> bool:
        """Emit an event"""
        return self.broker.publish(event_type, data, source)

    def on(self, event_type: str, handler: Callable) -> str:
        """Register event handler"""
        if event_type not in self.event_handlers:
            self.event_handlers[event_type] = []

        self.event_handlers[event_type].append(handler)
        return self.broker.subscribe(event_type, handler)

    def off(self, event_type: str, handler: Callable) -> bool:
        """Unregister event handler"""
        if event_type in self.event_handlers:
            if handler in self.event_handlers[event_type]:
                self.event_handlers[event_type].remove(handler)

        return self.broker.unsubscribe(event_type, handler)


class MessageQueue:
    """Reliable message queue with persistence"""

    def __init__(self, redis_host: str = 'localhost', redis_port: int = 6379):
        self.redis = redis.Redis(
            host=redis_host,
            port=redis_port,
            decode_responses=True
        )
        self.prefix = "aimran:queue:"

    def enqueue(self, queue_name: str, message: Dict[str, Any]) -> bool:
        """Add message to queue"""
        try:
            queue_key = f"{self.prefix}{queue_name}"
            self.redis.rpush(queue_key, json.dumps(message))
            return True
        except Exception as e:
            logger.error(f"❌ Failed to enqueue: {str(e)}")
            return False

    def dequeue(self, queue_name: str) -> Optional[Dict]:
        """Get message from queue"""
        try:
            queue_key = f"{self.prefix}{queue_name}"
            msg_json = self.redis.lpop(queue_key)
            if msg_json:
                return json.loads(msg_json)
            return None
        except Exception as e:
            logger.error(f"❌ Failed to dequeue: {str(e)}")
            return None

    def get_queue_length(self, queue_name: str) -> int:
        """Get queue length"""
        try:
            queue_key = f"{self.prefix}{queue_name}"
            return self.redis.llen(queue_key)
        except Exception as e:
            logger.error(f"❌ Failed to get length: {str(e)}")
            return 0

    def peek(self, queue_name: str, count: int = 10) -> List[Dict]:
        """Peek at queue messages without removing"""
        try:
            queue_key = f"{self.prefix}{queue_name}"
            messages = self.redis.lrange(queue_key, 0, count - 1)
            return [json.loads(msg) for msg in messages]
        except Exception as e:
            logger.error(f"❌ Failed to peek: {str(e)}")
            return []


# Global instances (lazy init — call init_ipc() explicitly)
_ipc_broker_instance = None
_event_bus_instance = None
_message_queue_instance = None

def get_ipc_broker() -> IPCBroker:
    """Get or create the IPC broker singleton"""
    global _ipc_broker_instance
    if _ipc_broker_instance is None:
        _ipc_broker_instance = IPCBroker()
    return _ipc_broker_instance

def get_event_bus() -> EventBus:
    """Get or create the event bus singleton"""
    global _event_bus_instance
    if _event_bus_instance is None:
        _event_bus_instance = EventBus(get_ipc_broker())
    return _event_bus_instance

def get_message_queue() -> MessageQueue:
    """Get or create the message queue singleton"""
    global _message_queue_instance
    if _message_queue_instance is None:
        _message_queue_instance = MessageQueue()
    return _message_queue_instance

def init_ipc():
    """Initialize all IPC components explicitly"""
    broker = get_ipc_broker()
    bus = get_event_bus()
    queue = get_message_queue()
    logger.info("✅ IPC Broker, Event Bus, and Message Queue initialized")
    return broker, bus, queue

# Backward-compatible aliases
ipc_broker = None
event_bus = None
message_queue = None

logger.info("✅ IPC Broker, Event Bus, and Message Queue module loaded (call init_ipc() to start)")
