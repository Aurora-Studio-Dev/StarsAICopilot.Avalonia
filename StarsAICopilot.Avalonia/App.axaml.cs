   using Avalonia;
   using Avalonia.Controls.ApplicationLifetimes;
   using Avalonia.Markup.Xaml;

   namespace StarsAICopilot.Avalonia
   {
       public partial class App : Application
       {
           public override void Initialize()
           {
               AvaloniaXamlLoader.Load(this);
           }

           public override void RegisterServices()
           {
               base.RegisterServices();
           }

           public override void OnFrameworkInitializationCompleted()
           {
               if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
               {
                   desktop.MainWindow = new MainWindow();
               }
               base.OnFrameworkInitializationCompleted();
           }
       }
   }
   