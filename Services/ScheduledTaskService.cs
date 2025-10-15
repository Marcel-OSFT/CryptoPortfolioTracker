using Microsoft.Win32.TaskScheduler;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace CryptoPortfolioTracker.Services
{
    internal class ScheduledTaskService
    {
        private readonly string _scheduledTaskName;
        private readonly string _exePath;
        private readonly string _triggerTime;
        private readonly string _taskDescription;
        private readonly Func<string, string> _getLocalizedString;

        public ScheduledTaskService(string scheduledTaskName, string exePath, string triggerTime, string taskDescription, Func<string, string> getLocalizedString)
        {
            _scheduledTaskName = scheduledTaskName;
            _exePath = exePath;
            _triggerTime = triggerTime;
            _taskDescription = taskDescription;
            _getLocalizedString = getLocalizedString;
        }

        public async System.Threading.Tasks.Task SetupScheduledTaskAsync(Window? splash)
        {
            var registered = await IsTaskRegisteredAsync();
            if (!registered)
            {
                if (!AdminCheck.IsRunAsAdmin())
                {
                    var dialog = new ContentDialog
                    {
                        Title = _getLocalizedString("Messages_ScheduledTask_Title"),
                        Content = _getLocalizedString("Messages_ScheduledTask_Explainer"),
                        XamlRoot = splash?.Content.XamlRoot,
                        CloseButtonText = "OK"
                    };
                    await dialog.ShowAsync();
                    RestartAsAdmin();
                }
                else
                {
                    RegisterScheduledTask();
                }
            }
        }

        private async Task<bool> IsTaskRegisteredAsync()
        {
            using (TaskService ts = new TaskService())
            {
                TaskFolder folder = ts.GetFolder(@"\");
                var task = folder.Tasks.FirstOrDefault(t => t.Name == _scheduledTaskName);
                return (task != null);
            }
        }

        private void RegisterScheduledTask()
        {
            using (TaskService ts = new TaskService())
            {
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = _taskDescription;
                td.Principal.LogonType = TaskLogonType.InteractiveToken;
                td.Principal.UserId = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                td.Principal.RunLevel = TaskRunLevel.Highest;

                var repetition = new RepetitionPattern(TimeSpan.FromHours(1), TimeSpan.FromHours(20));
                var dailyTrigger = new DailyTrigger { Repetition = repetition, StartBoundary = DateTime.Today.AddHours(3) };
                td.Triggers.Add(dailyTrigger);

                td.Actions.Add(new ExecAction(_exePath, null, null));
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.StopIfGoingOnBatteries = false;
                td.Settings.RunOnlyIfIdle = false;
                td.Settings.IdleSettings.StopOnIdleEnd = false;

                ts.RootFolder.RegisterTaskDefinition(_scheduledTaskName, td);
            }
        }

        private void RestartAsAdmin()
        {
            var exeName = Process.GetCurrentProcess().MainModule.FileName;
            var startInfo = new ProcessStartInfo(exeName)
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            try
            {
                Process.Start(startInfo);
                Application.Current.Exit();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to restart application as administrator: {ex.Message}",
                    CloseButtonText = "OK"
                };
                dialog.ShowAsync();
            }
        }
    }
}
