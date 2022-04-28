using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace BasicPluginMa
{
    public class FollowupPlugin : IPlugin
    {
		public void Execute(IServiceProvider serviceProvider)
		{
			//获取 tracing service 
			ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

			//从services provider获取执行的上下文
			IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

			//输入的参数包含所有请求的数据
			if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity) 
			{
				//从输入的参数中获取target实体
				Entity entity = (Entity)context.InputParameters["Target"];

				//进行web请求的时候要获取organization services reference
				IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
				IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

				try
				{
					//在7天内创建一个活动任务来跟踪用户
					Entity followup = new Entity("task");

					followup["subject"] = "send email to the new customer";
					followup["description"] = "Follow up with the customwer, check if there are any new issues need to do";
					followup["scheduledstart"] = DateTime.Now.AddDays(7);
					followup["scheduledend"] = DateTime.Now.AddDays(7);
					followup["category"] = context.PrimaryEntityName;

					//refer to the account in the task activity
					if (context.OutputParameters.Contains("id")) {
						Guid regardingobjectid = new Guid(context.OutputParameters["id"].ToString());
						string regardingobjectidType = "account";

						followup["regardingobjectid"] = new EntityReference(regardingobjectidType, regardingobjectid);
					}

					// 创建任务
					tracingService.Trace("followupplugin create the task activity");
					service.Create(followup);

				}
				catch (FaultException<OrganizationServiceFault> ex)
				{
					throw new InvalidPluginExecutionException("an error occured in Followup plugin", ex);
				}

				catch (Exception e) {
					tracingService.Trace("FollowupPlugin: {0}", e.ToString());
					throw;
				}
			}
		}
	}
}
