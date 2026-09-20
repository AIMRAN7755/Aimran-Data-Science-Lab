namespace AIMRAN_Data_Science_Lab
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "AIMRAN Data Science Lab" };
        }

        /// <summary>
        /// Called when app is destroyed or closed.
        /// Gracefully shuts down all engines and cleans up resources.
        /// </summary>
        protected override void OnDestroying()
        {
            base.OnDestroying();

            // Gracefully shutdown all engines before app closes
            _ = AppLifecycleManager.ShutdownAsync();
        }
    }
}
