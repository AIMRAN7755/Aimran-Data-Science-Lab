"""Setup validation and prerequisites checker"""

import sys
import subprocess
import importlib.util
import logging
from pathlib import Path
from tabulate import tabulate
import platform

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class PrerequisiteChecker:
    """Check system prerequisites"""

    def __init__(self):
        self.results = {
            'python': {},
            'system_tools': {},
            'python_packages': {},
            'directories': {},
            'files': {},
        }
        self.all_passed = True

    # Python Version Check
    def check_python_version(self, min_version: str = "3.9.0"):
        """Check Python version"""
        current_version = platform.python_version()
        parts = [int(x) for x in min_version.split('.')]
        current_parts = [int(x) for x in current_version.split('.')]

        passed = current_parts >= parts
        self.results['python']['version'] = {
            'required': min_version,
            'current': current_version,
            'passed': passed,
            'message': f"Python {current_version} {'✅' if passed else '❌'}"
        }

        if not passed:
            self.all_passed = False
        return passed

    # System Tools Check
    def check_system_tool(self, tool_name: str):
        """Check if system tool is available"""
        try:
            result = subprocess.run(
                [tool_name, '--version'] if tool_name != 'make' else ['make', '--version'],
                capture_output=True,
                text=True,
                timeout=5
            )

            passed = result.returncode == 0
            version = result.stdout.split('\n')[0] if passed else "Not found"

            self.results['system_tools'][tool_name] = {
                'available': passed,
                'version': version,
                'message': f"{tool_name} {'✅' if passed else '⚠️'}"
            }

            if tool_name in ['git', 'docker']:  # Critical tools
                if not passed:
                    self.all_passed = False

            return passed
        except FileNotFoundError:
            self.results['system_tools'][tool_name] = {
                'available': False,
                'message': f"{tool_name} ⚠️ (not in PATH)"
            }
            return False

    # Python Package Check
    def check_python_package(self, package_name: str, import_name: str = None, 
                           critical: bool = False):
        """Check if Python package is installed"""
        import_name = import_name or package_name

        try:
            __import__(import_name)
            module = importlib.util.find_spec(import_name)
            version = getattr(__import__(import_name), '__version__', 'unknown')

            self.results['python_packages'][package_name] = {
                'installed': True,
                'version': version,
                'message': f"{package_name} ✅"
            }
            return True
        except ImportError:
            self.results['python_packages'][package_name] = {
                'installed': False,
                'message': f"{package_name} ❌"
            }

            if critical:
                self.all_passed = False

            return False

    # Directory Check
    def check_directory(self, path: str, create: bool = True):
        """Check if directory exists"""
        p = Path(path)

        if p.exists():
            self.results['directories'][path] = {
                'exists': True,
                'message': f"{path} ✅"
            }
            return True
        else:
            if create:
                try:
                    p.mkdir(parents=True, exist_ok=True)
                    self.results['directories'][path] = {
                        'exists': True,
                        'created': True,
                        'message': f"{path} ✅ (created)"
                    }
                    return True
                except Exception as e:
                    self.results['directories'][path] = {
                        'exists': False,
                        'error': str(e),
                        'message': f"{path} ❌ (failed to create)"
                    }
                    self.all_passed = False
                    return False
            else:
                self.results['directories'][path] = {
                    'exists': False,
                    'message': f"{path} ❌"
                }
                self.all_passed = False
                return False

    # File Check
    def check_file(self, path: str):
        """Check if file exists"""
        p = Path(path)

        if p.exists():
            self.results['files'][path] = {
                'exists': True,
                'message': f"{path} ✅"
            }
            return True
        else:
            self.results['files'][path] = {
                'exists': False,
                'message': f"{path} ⚠️"
            }
            return False

    def run_all_checks(self):
        """Run comprehensive prerequisite checks"""
        print("=" * 70)
        print("AIMRAN DATA SCIENCE LAB - SETUP VALIDATOR")
        print("=" * 70)
        print()

        # Python checks
        print("📍 PYTHON VERSION CHECK")
        self.check_python_version("3.9.0")
        self._print_section_results('python')
        print()

        # System tools
        print("🛠️ SYSTEM TOOLS CHECK")
        for tool in ['git', 'docker', 'make', 'python', 'pip']:
            self.check_system_tool(tool)
        self._print_section_results('system_tools')
        print()

        # Critical Python packages
        print("📦 CRITICAL PYTHON PACKAGES")
        critical_packages = [
            ('pandas', 'pandas', True),
            ('numpy', 'numpy', True),
            ('scikit-learn', 'sklearn', True),
            ('torch', 'torch', True),
            ('fastapi', 'fastapi', True),
            ('sqlalchemy', 'sqlalchemy', True),
            ('psutil', 'psutil', True),
        ]

        for package, import_name, critical in critical_packages:
            self.check_python_package(package, import_name, critical)
        self._print_section_results('python_packages')
        print()

        # Additional packages
        print("📦 OPTIONAL PYTHON PACKAGES")
        optional_packages = [
            ('mlflow', 'mlflow', False),
            ('dvc', 'dvc', False),
            ('streamlit', 'streamlit', False),
            ('jupyter', 'jupyter', False),
            ('plotly', 'plotly', False),
        ]

        for package, import_name, critical in optional_packages:
            self.check_python_package(package, import_name, critical)
        self._print_section_results('python_packages', start_idx=len(critical_packages))
        print()

        # Directories
        print("📂 DIRECTORY STRUCTURE")
        directories = [
            'data/raw',
            'data/processed',
            'models',
            'logs',
            'notebooks',
            'tests',
        ]

        for directory in directories:
            self.check_directory(directory, create=True)
        self._print_section_results('directories')
        print()

        # Configuration files
        print("⚙️ CONFIGURATION FILES")
        files_to_check = [
            '.env.example',
            'requirements.txt',
            'setup.py',
            'pyproject.toml',
        ]

        for file in files_to_check:
            self.check_file(file)
        self._print_section_results('files')
        print()

        # Summary
        self._print_summary()

    def _print_section_results(self, section: str, start_idx: int = 0):
        """Print results for a section"""
        results = self.results[section]

        if not results:
            print("  No checks performed")
            return

        items = list(results.items())
        if start_idx > 0:
            items = items[start_idx:]

        for key, value in items:
            if isinstance(value, dict) and 'message' in value:
                print(f"  {value['message']}")
                if 'version' in value and value.get('available'):
                    print(f"    Version: {value['version']}")

    def _print_summary(self):
        """Print summary"""
        print("=" * 70)

        if self.all_passed:
            print("✅ ALL CHECKS PASSED - System is ready for development!")
            print()
            print("Next steps:")
            print("  1. make install-dev       - Install development dependencies")
            print("  2. make docker-build      - Build Docker containers")
            print("  3. make docker-up         - Start services")
            print("  4. make dashboard         - Start Streamlit dashboard")
            return 0
        else:
            print("⚠️ SOME CHECKS FAILED - Please review and fix the issues above")
            print()
            print("Recommendations:")
            print("  - Install missing critical packages")
            print("  - Ensure Docker is installed and running")
            print("  - Check Python version compatibility")
            return 1


def main():
    """Run setup validation"""
    checker = PrerequisiteChecker()
    exit_code = checker.run_all_checks()
    sys.exit(exit_code)


if __name__ == '__main__':
    main()
