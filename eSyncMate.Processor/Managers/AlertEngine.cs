using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Hangfire;

namespace eSyncMate.Processor.Managers
{
    public class AlertEngine
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private static Dictionary<int, CustomerAlerts> currentAlerts = new Dictionary<int, CustomerAlerts>();

        public AlertEngine(IConfiguration config)
        {
            _config = config;
            //_logger = logger;
        }

        public void Execute(int customerAlertID)
        {
            CustomerAlerts customerAlerts = new CustomerAlerts();

            try
            {
                customerAlerts.UseConnection(CommonUtils.ConnectionString);

                customerAlerts.Id = customerAlertID;
                if (!customerAlerts.GetObject().IsSuccess)
                {
                    this._logger?.LogError($"Invalid Alert! [{customerAlertID}]");
                    return;
                }

                if (currentAlerts.ContainsKey(customerAlertID))
                {
                    //customerAlerts.SaveLog(Declarations.LogTypeEnum.Info, "This route is already in execution.", "", 1);
                    return;
                }

                currentAlerts[customerAlertID] = customerAlerts;

                if (customerAlerts.AlertsConfiguration.AlertType.ToUpper() == "CUSTOMER")
                {
                    CustomerWiseAlert.Execute(_config, _logger, customerAlerts);
                }

            }
            catch (Exception ex)
            {
                this._logger?.LogCritical(ex, ex.Message);
            }
            finally
            {
                if (currentAlerts.ContainsKey(customerAlertID))
                    currentAlerts.Remove(customerAlertID);
            }
        }

        public void Schedule(int alertId)
        {
            CustomerAlerts alert = new CustomerAlerts();

            try
            {
                alert.UseConnection(CommonUtils.ConnectionString);

                alert.Id = alertId;
                if (!alert.GetObject().IsSuccess)
                {
                    this._logger?.LogError($"Invalid Alert! [{alertId}]");
                    return;
                }

                this.SetupRouteJob(alert);
            }
            catch (Exception ex)
            {
                this._logger?.LogCritical(ex, ex.Message);
            }
        }

        private void SetupRouteJob(CustomerAlerts route)
        {

            AlertEngine l_Engine = this;

            if (route.FrequencyType == "Minutely")
            {
                RecurringJob.AddOrUpdate($"Alert [{route.Id}]", () => l_Engine.Execute(route.Id), $"*/{route.RepeatCount} * * * *", TimeZoneInfo.Local);
            }
            else if (route.FrequencyType == "Hourly")
            {
                RecurringJob.AddOrUpdate($"Alert [{route.Id}]", () => l_Engine.Execute(route.Id), $"0 */{route.RepeatCount} * * *", TimeZoneInfo.Local);
            }
            else if (route.FrequencyType == "Daily")
            {
                if (string.IsNullOrEmpty(route.ExecutionTime))
                {
                    RecurringJob.AddOrUpdate($"Alert [{route.Id}]", () => l_Engine.Execute(route.Id), Cron.Daily, TimeZoneInfo.Local);
                }
                else
                {
                    foreach (string time in route.ExecutionTime.Split(","))
                    {
                        string[] split = time.Split(":");
                        RecurringJob.AddOrUpdate($"Alert [{route.Id}] at [{time}]", () => l_Engine.Execute(route.Id), Cron.Daily(Convert.ToInt32(split[0]), Convert.ToInt32(split[1])), TimeZoneInfo.Local);
                    }
                }
            }
            else if (route.FrequencyType == "Weekly")
            {
                foreach (string weekday in route.WeekDays.Split(","))
                {
                    if (string.IsNullOrEmpty(route.ExecutionTime))
                    {
                        RecurringJob.AddOrUpdate($"Alert [{route.Id}] on [{weekday}]", () => l_Engine.Execute(route.Id), Cron.Weekly(Enum.Parse<DayOfWeek>(weekday)), TimeZoneInfo.Local);
                    }
                    else
                    {
                        foreach (string time in route.ExecutionTime.Split(","))
                        {
                            string[] split = time.Split(":");
                            RecurringJob.AddOrUpdate($"Alert [{route.Id}] on [{weekday}] at [{time}]", () => l_Engine.Execute(route.Id), Cron.Weekly(Enum.Parse<DayOfWeek>(weekday), Convert.ToInt32(split[0]), Convert.ToInt32(split[1])), TimeZoneInfo.Local);
                        }
                    }
                }
            }
            else if (route.FrequencyType == "Monthly")
            {
                foreach (string day in route.DayOfMonth.Split(","))
                {
                    if (string.IsNullOrEmpty(route.ExecutionTime))
                    {
                        RecurringJob.AddOrUpdate($"Alert [{route.Id}] on [{day}]", () => l_Engine.Execute(route.Id), Cron.Monthly(Convert.ToInt32(day)), TimeZoneInfo.Local);
                    }
                    else
                    {
                        foreach (string time in route.ExecutionTime.Split(","))
                        {
                            string[] split = time.Split(":");
                            RecurringJob.AddOrUpdate($"Alert [{route.Id}] on [{day}] at [{time}]", () => l_Engine.Execute(route.Id), Cron.Monthly(Convert.ToInt32(day), Convert.ToInt32(split[0]), Convert.ToInt32(split[1])), TimeZoneInfo.Local);
                        }
                    }
                }
            }
        }

        public void RemoveRouteJob(CustomerAlerts route)
        {
            if (route.FrequencyType == "Minutely")
            {
                RecurringJob.RemoveIfExists($"Alert [{route.Id}]");
            }
            else if (route.FrequencyType == "Hourly")
            {
                RecurringJob.RemoveIfExists($"Alert [{route.Id}]");
            }
            else if (route.FrequencyType == "Daily")
            {
                if (string.IsNullOrEmpty(route.ExecutionTime))
                {
                    RecurringJob.RemoveIfExists($"Alert [{route.Id}]");
                }
                else
                {
                    foreach (string time in route.ExecutionTime.Split(","))
                    {
                        RecurringJob.RemoveIfExists($"Alert [{route.Id}] at [{time}]");
                    }
                }
            }
            else if (route.FrequencyType == "Weekly")
            {
                foreach (string weekday in route.WeekDays.Split(","))
                {
                    if (string.IsNullOrEmpty(route.ExecutionTime))
                    {
                        RecurringJob.RemoveIfExists($"Alert [{route.Id}] on [{weekday}]");
                    }
                    else
                    {
                        foreach (string time in route.ExecutionTime.Split(","))
                        {
                            RecurringJob.RemoveIfExists($"Alert [{route.Id}] on [{weekday}] at [{time}]");
                        }
                    }
                }
            }
            else if (route.FrequencyType == "Monthly")
            {
                foreach (string day in route.DayOfMonth.Split(","))
                {
                    if (string.IsNullOrEmpty(route.ExecutionTime))
                    {
                        RecurringJob.RemoveIfExists($"Alert [{route.Id}] on [{day}]");
                    }
                    else
                    {
                        foreach (string time in route.ExecutionTime.Split(","))
                        {
                            RecurringJob.RemoveIfExists($"Alert [{route.Id}] on [{day}] at [{time}]");
                        }
                    }
                }
            }
        }

        private void TriggerJob(CustomerAlerts route)
        {

            AlertEngine l_Engine = this;

            if (route.FrequencyType == "Minutely")
            {
                RecurringJob.Trigger(route.JobID);
            }
            else if (route.FrequencyType == "Hourly")
            {
                RecurringJob.Trigger(route.JobID);
            }
        }
    }
}
