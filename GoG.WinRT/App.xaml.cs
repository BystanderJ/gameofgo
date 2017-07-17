using System;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using GoG.WinRT.Services;
using Microsoft.HockeyApp;
using Microsoft.Practices.Unity;
using Prism.Mvvm;

namespace GoG.WinRT
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App
    {
        public App()
        {
            HockeyClient.Current.Configure("98be9ebbbbd7439cbb22b51a29fd6e51")
                .SetExceptionDescriptionLoader(
                    ex =>
                    {
                        var msg = new StringBuilder();

                        ex = ex.GetBaseException();

                        msg.AppendLine("Base Exception Type: " + ex.GetType().FullName);

                        if (Container != null)
                        {
                            var nav = NavigationService;
                            if (nav != null)
                                msg.AppendLine("INavigationService.CanGoBack() == " + nav.CanGoBack());

                        }

                        if (Window.Current == null)
                            msg.AppendLine("Window.Current is NULL.");
                        else
                        {
                            if (Window.Current.Content == null)
                                msg.AppendLine("Window.Current.Content is NULL.");
                            else
                            {
                                var rf = Window.Current.Content as Frame;
                                if (rf == null)
                                    msg.AppendLine("Root frame is NULL.");
                                else
                                {
                                    msg.AppendLine("rootFrame.CurrentSourcePageType is " + rf.CurrentSourcePageType.Name);
                                    if (rf.BackStack != null)
                                        foreach (var p in rf.BackStack)
                                            msg.AppendLine("Back page: " + p.SourcePageType.Name);
                                    if (rf.ForwardStack != null)
                                        foreach (var p in rf.ForwardStack)
                                            msg.AppendLine("Forward page: " + p.SourcePageType.Name);
                                }
                            }
                        }

                        if (ex.Source != null)
                            msg.AppendLine("Source: " + ex.Source);

                        if (ex.Data != null && ex.Data.Keys.Count > 0)
                            foreach (var d in ex.Data.Keys)
                                msg.AppendLine(d.ToString());

                        if (ex.InnerException != null)
                            msg.AppendLine(
                                $"INNER Exception HResult: {ex.HResult}\nINNER EXCEPTION DETAILS:\n{ex.InnerException}");

                        msg.AppendLine($"Message: {ex.Message}\nException HResult: {ex.HResult}");

                        return msg.ToString();
                    });

            InitializeComponent();

            UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            
            RequestedTheme = ApplicationTheme.Dark;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //e.Handled = true;
            
            //var msg = $"Unhandled exception:\nType: {e.GetType().Name}\nMessage: {e.Message}";
        }

        //protected override void OnRegisterKnownTypesForSerialization()
        //{
        //    base.OnRegisterKnownTypesForSerialization();

        //    // These types are used in the game state.
        //    //SessionStateService.RegisterKnownType(typeof(GoGame));
        //    //SessionStateService.RegisterKnownType(typeof(GoPlayer));
        //    //SessionStateService.RegisterKnownType(typeof(PlayerType));
        //    //SessionStateService.RegisterKnownType(typeof(GoGameStatus));
        //    //SessionStateService.RegisterKnownType(typeof(GoColor));
        //    //SessionStateService.RegisterKnownType(typeof(MoveType));
        //    //SessionStateService.RegisterKnownType(typeof(GoOperation));
        //    //SessionStateService.RegisterKnownType(typeof(MoveType));
        //    //SessionStateService.RegisterKnownType(typeof(GoResultCode));
        //    //SessionStateService.RegisterKnownType(typeof(GoMoveHistoryItem));
        //    //SessionStateService.RegisterKnownType(typeof(GoMove));
        //    //SessionStateService.RegisterKnownType(typeof(GoMoveResult));
        //}

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (args.PrelaunchActivated)
                return;

            base.OnLaunched(args);
        }

        /// <summary>
        /// Required override. Generally you do your initial navigation to launch page, or 
        /// to the page approriate based on a search, sharing, or secondary tile launch of the app
        /// </summary>
        /// <param name="args">The launch arguments passed to the application</param>
        protected override Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
        {
            // Use the logical name for the view to navigate to. The default convention
            // in the NavigationService will be to append "Page" to the name and look 
            // for that page in a .Views child namespace in the project. IF you want another convention
            // for mapping view names to view types, you can override 
            // the MvvmAppBase.GetPageNameToTypeResolver method
            if (args.PreviousExecutionState != ApplicationExecutionState.Terminated)
                NavigationService.Navigate("Main", null);

            return Task.FromResult<object>(null);
        }        

        /// <summary>
        /// This is the place you initialize your services and set default factory or default resolver for the view model locator
        /// </summary>
        /// <param name="args">The same launch arguments passed when the app starts.</param>
        protected override Task OnInitializeAsync(IActivatedEventArgs args)
        {
            // Set a factory for the ViewModelLocator to use the container to construct view models so their 
            // dependencies get injected by the container
            ViewModelLocationProvider.SetDefaultViewModelFactory(viewModelType => Resolve(viewModelType));

            var repo = Container.Resolve<IRepository>();
            repo.Initialize();

            return Task.FromResult<object>(null);
        }
        
        protected override object Resolve(Type type)
        {
            var obj = Container.Resolve(type);
            return obj;
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            // Register any app specific types with the container
            Container.RegisterType<IRepository, SqliteRepository>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IGameEngine, FuegoGameEngine>(new ContainerControlledLifetimeManager());
        }
    }
}