using System.Net.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Actions;
using Partnerly.Automation.Authoring;
using Partnerly.Automation.Conditions;
using Partnerly.Automation.Connectors;
using Partnerly.Automation.Engine;
using Partnerly.Automation.Integration;
using Partnerly.Automation.Persistence;
using Partnerly.Automation.Registry;
using Partnerly.Automation.Scheduler;
using Partnerly.Automation.Security;
using Partnerly.Automation.Triggers;

namespace Partnerly.Automation.Hosting
{
    public static class AutomationServiceCollectionExtensions
    {
        public static IServiceCollection AddPartnerlyAutomation(this IServiceCollection services, bool useDatabase = false)
        {
            services.AddDataProtection();
            services.AddSingleton<IAutomationClock, SystemAutomationClock>();
            if (useDatabase)
            {
                services.AddScoped<IAutomationStore, AutomationStore>();
                services.AddHostedService<AutomationWorker>();
            }
            else
            {
                services.AddSingleton<InMemoryAutomationStore>();
                services.AddSingleton<IAutomationStore>(sp => sp.GetRequiredService<InMemoryAutomationStore>());
            }

            services.AddSingleton<IVariableBinder, VariableBinder>();
            services.AddSingleton<IConditionEvaluator, ConditionEvaluator>();
            services.AddSingleton<IPublicHttpGuard, AutomationHttpGuard>();
            services.AddSingleton(sp => new GraphValidator(
                sp.GetRequiredService<TriggerRegistry>(),
                sp.GetRequiredService<ActionRegistry>()));
            services.AddSingleton<CronScheduleCalculator>();
            services.AddSingleton<IAutomationRunNotifier, NullAutomationRunNotifier>();
            services.AddScoped<ISecretProtector, SecretProtector>();
            services.AddScoped<IAutomationScheduler, ScheduleArmer>();
            services.AddScoped<StepRunner>();
            services.AddScoped<WorkflowExecutor>();
            services.AddScoped<IAutomationRunService, AutomationRunService>();
            services.AddScoped<IAutomationAuthoringService, AutomationAuthoringService>();
            services.AddScoped<AutomationTriggerDispatcher>();
            services.AddScoped<IAutomationChangeSink, AutomationSaveChangesInterceptor>();
            services.AddScoped<AutomationWebhookIngestService>();

            RegisterTriggers(services);
            RegisterActions(services);
            services.AddSingleton(sp =>
            {
                var registry = CreateTriggerRegistry();
                foreach (var module in sp.GetServices<IAutomationModule>())
                {
                    module.RegisterTriggers(registry);
                }

                return registry;
            });
            services.AddSingleton(sp =>
            {
                var registry = CreateActionRegistry();
                foreach (var module in sp.GetServices<IAutomationModule>())
                {
                    module.RegisterActions(registry);
                }

                return registry;
            });

            if (useDatabase)
            {
                services.AddScoped<IEntityConnector, UserConnector>();
                services.AddScoped<IEntityConnector, SupportTicketConnector>();
                services.AddScoped<IEntityConnector, NotificationConnector>();
                services.AddScoped<IEntityConnector, UserGroupConnector>();
                services.AddScoped<IEntityConnector, EmailTemplateConnector>();
                services.AddScoped(sp => new EntityRegistry(sp.GetServices<IEntityConnector>()));
                services.AddScoped<Partnerly.Automation.Web.AutomationLookupService>();
                services.AddScoped<Partnerly.Automation.Web.AutomationFavoritesService>();
                services.AddScoped<IAutomationAccessService, CurrentUserAutomationAccessService>();
                services.AddScoped<IAutomationRunNotifier, AutomationRunNotifier>();
                services.AddTransient<AutomationEventBridge>();
                services.AddHttpClient(OutboundHttp.ClientName, client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(20);
                }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = false
                });
            }
            else
            {
                services.AddSingleton(_ => new EntityRegistry(Array.Empty<IEntityConnector>()));
                services.AddScoped<IAutomationAccessService>(_ => new AutomationAccessService(Guid.Empty, false));
            }

            return services;
        }

        private static void RegisterTriggers(IServiceCollection services)
        {
            services.AddTransient<ManualTriggerHandler>();
            services.AddTransient<ScheduleTriggerHandler>();
            services.AddTransient<IntervalTriggerHandler>();
            services.AddTransient<EntityCreatedTriggerHandler>();
            services.AddTransient<EntityUpdatedTriggerHandler>();
            services.AddTransient<EntityDeletedTriggerHandler>();
            services.AddTransient<WebhookTriggerHandler>();
            services.AddTransient<EventTriggerHandler>();
            services.AddTransient<DelayTriggerHandler>();
        }

        private static void RegisterActions(IServiceCollection services)
        {
            services.AddTransient<SendEmailActionHandler>();
            services.AddTransient<SendNotificationActionHandler>();
            services.AddTransient<CreateRecordActionHandler>();
            services.AddTransient<UpdateRecordActionHandler>();
            services.AddTransient<FindRecordActionHandler>();
            services.AddTransient<DeleteRecordActionHandler>();
            services.AddTransient<DelayActionHandler>();
            services.AddTransient<WaitUntilActionHandler>();
            services.AddTransient<ConditionalBranchActionHandler>();
            services.AddTransient<SwitchActionHandler>();
            services.AddTransient<SetVariableActionHandler>();
            services.AddTransient<HttpRequestActionHandler>();
            services.AddTransient<WebhookActionHandler>();
            services.AddTransient<ExecuteWorkflowActionHandler>();
        }

        private static TriggerRegistry CreateTriggerRegistry()
        {
            var registry = new TriggerRegistry();
            registry.Register<ManualTriggerHandler>(AutomationTypeCodes.Manual);
            registry.Register<ScheduleTriggerHandler>(AutomationTypeCodes.Schedule);
            registry.Register<IntervalTriggerHandler>(AutomationTypeCodes.Interval);
            registry.Register<EntityCreatedTriggerHandler>(AutomationTypeCodes.EntityCreated);
            registry.Register<EntityUpdatedTriggerHandler>(AutomationTypeCodes.EntityUpdated);
            registry.Register<EntityDeletedTriggerHandler>(AutomationTypeCodes.EntityDeleted);
            registry.Register<WebhookTriggerHandler>(AutomationTypeCodes.Webhook);
            registry.Register<EventTriggerHandler>(AutomationTypeCodes.Event);
            registry.Register<DelayTriggerHandler>(AutomationTypeCodes.DelayTrigger);
            return registry;
        }

        private static ActionRegistry CreateActionRegistry()
        {
            var registry = new ActionRegistry();
            registry.Register<SendEmailActionHandler>(AutomationTypeCodes.SendEmail);
            registry.Register<SendNotificationActionHandler>(AutomationTypeCodes.SendNotification);
            registry.Register<CreateRecordActionHandler>(AutomationTypeCodes.CreateRecord);
            registry.Register<UpdateRecordActionHandler>(AutomationTypeCodes.UpdateRecord);
            registry.Register<FindRecordActionHandler>(AutomationTypeCodes.FindRecord);
            registry.Register<DeleteRecordActionHandler>(AutomationTypeCodes.DeleteRecord);
            registry.Register<DelayActionHandler>(AutomationTypeCodes.Delay);
            registry.Register<WaitUntilActionHandler>(AutomationTypeCodes.WaitUntil);
            registry.Register<ConditionalBranchActionHandler>(AutomationTypeCodes.Branch);
            registry.Register<SwitchActionHandler>(AutomationTypeCodes.Switch);
            registry.Register<SetVariableActionHandler>(AutomationTypeCodes.SetVariable);
            registry.Register<HttpRequestActionHandler>(AutomationTypeCodes.HttpRequest);
            registry.Register<WebhookActionHandler>(AutomationTypeCodes.WebhookAction);
            registry.Register<ExecuteWorkflowActionHandler>(AutomationTypeCodes.ExecuteWorkflow);
            return registry;
        }
    }
}
