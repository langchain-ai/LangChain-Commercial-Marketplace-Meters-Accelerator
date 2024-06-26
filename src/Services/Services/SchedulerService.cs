﻿using ManagedApplicationScheduler.DataAccess.Contracts;
using ManagedApplicationScheduler.DataAccess.Entities;
using ManagedApplicationScheduler.Services.Contracts;
using ManagedApplicationScheduler.Services.Helpers;
using ManagedApplicationScheduler.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManagedApplicationScheduler.Services.Services
{
    public class SchedulerService
    {
        private IScheduledTasksRepository schedulerRepository;
        /// <summary>
        /// Email Service Interface
        /// </summary>
        private IEmailService emailService;
        /// <summary>
        /// Email Helper utility
        /// </summary>
        private EmailHelper emailHelper;
        public SchedulerService(IScheduledTasksRepository schedulerRepository, IEmailService emailService, IApplicationConfigurationRepository applicationConfigurationRepository)
        {
            this.schedulerRepository = schedulerRepository;
            if (emailService != null)
            {
                this.emailService = emailService;
            }
            if (applicationConfigurationRepository!=null)
            {
                this.emailHelper = new EmailHelper(applicationConfigurationRepository);
            }
        }
        public int SaveScheduler(ScheduledTasksModel task)
        {
            var entity = new ScheduledTasks();
            entity.id = task.id;
            entity.ScheduledTaskName = task.ScheduledTaskName;
            entity.StartDate = task.StartDate;
            entity.NextRunTime = task.NextRunTime;
            entity.PlanId = task.PlanId;
            entity.Dimension = task.Dimension;
            entity.Quantity = task.Quantity;
            entity.ResourceUri = task.ResourceUri;
            entity.Status = task.Status;
            entity.Frequency = task.Frequency;

            return this.schedulerRepository.Save(entity);

        }

        public void UpdateScheduler(ScheduledTasksModel task)
        {
            var entity = this.schedulerRepository.Get(task.id);
            entity.ScheduledTaskName = task.ScheduledTaskName;
            entity.StartDate = task.StartDate;
            entity.NextRunTime = task.NextRunTime;
            entity.PlanId = task.PlanId;
            entity.Dimension = task.Dimension;
            entity.Quantity = task.Quantity;
            entity.ResourceUri = task.ResourceUri;
            entity.Status = task.Status;
            entity.Frequency = task.Frequency;
            this.schedulerRepository.Update(entity);

        }

        public void DeleteScheduler(string id)
        {

            var entity = this.schedulerRepository.Get(id);
            this.schedulerRepository.Remove(entity);

        }



        public ScheduledTasksModel GetSchedulerByID(string id)
        {
            var entity = new ScheduledTasksModel();
            var task = this.schedulerRepository.Get(id);
            entity.id = task.id;
            entity.ScheduledTaskName = task.ScheduledTaskName;
            entity.StartDate = task.StartDate;
            entity.NextRunTime = task.NextRunTime;
            entity.PlanId = task.PlanId;
            entity.Dimension = task.Dimension;
            entity.Quantity = task.Quantity;
            entity.ResourceUri = task.ResourceUri;
            entity.Status = task.Status;
            entity.Frequency = task.Frequency;

            return entity;
        }

        public List<ScheduledTasksModel> GetAllSchedulersTasks()
        {
            var schedulers = new List<ScheduledTasksModel>();
            var tasks = this.schedulerRepository.GetAll();
            foreach (var task in tasks)
            {
                var entity = new ScheduledTasksModel();
                entity.id = task.id;
                entity.ScheduledTaskName = task.ScheduledTaskName;
                entity.StartDate = task.StartDate;
                entity.NextRunTime = task.NextRunTime;
                entity.PlanId = task.PlanId;
                entity.Dimension = task.Dimension;
                entity.Quantity = task.Quantity;
                entity.ResourceUri = task.ResourceUri;
                entity.Status = task.Status;
                entity.Frequency = task.Frequency;
                schedulers.Add(entity);
            }

            return schedulers;
        }

        public List<ScheduledTasksModel> GetSchedulersTasksBySubscription(string resourceUri)
        {
            var schedulers = new List<ScheduledTasksModel>();
            var tasks = this.schedulerRepository.GetAll().Where(s => s.ResourceUri == resourceUri).ToList();


            foreach (var task in tasks)
            {
                var entity = new ScheduledTasksModel();
                entity.id = task.id;
                entity.ScheduledTaskName = task.ScheduledTaskName;
                entity.StartDate = task.StartDate;
                entity.NextRunTime = task.NextRunTime;
                entity.PlanId = task.PlanId;
                entity.Dimension = task.Dimension;
                entity.Quantity = task.Quantity;
                entity.ResourceUri = task.ResourceUri;
                entity.Status = task.Status;
                entity.Frequency = task.Frequency;
                schedulers.Add(entity);
            }

            return schedulers;
        }

        public List<ScheduledTasksModel> GetEnabledSchedulersTasks()
        {
            var schedulers = GetAllSchedulersTasks();

            var enabledTasks = schedulers.Where(s => s.Status == SchedulerStatusEnum.Scheduled.ToString() && !(s.ResourceUri.Contains("managedclusters"))).ToList();

            return enabledTasks;
        }

        public List<ScheduledTasksModel> GetEnabledSchedulersTasksBySubscription(string resourceUri)
        {
            var schedulers = GetAllSchedulersTasks();

            var enabledTasks = schedulers.Where(s => s.Status == SchedulerStatusEnum.Scheduled.ToString() && s.ResourceUri == resourceUri).ToList();

            return enabledTasks;
        }


        public void SendSchedulerEmail(ScheduledTasksModel schedulerTask, string status,string responseBody)
        {
            var emailContent = new EmailContentModel();

            if ((status == "Accepted") || (status == "Missing"))
            {
                //Success
                emailContent = this.emailHelper.PrepareMeteredEmailContent(schedulerTask.ScheduledTaskName, schedulerTask.ResourceUri, status, responseBody);
            }
            else
            {
                //Faliure
                emailContent = this.emailHelper.PrepareMeteredEmailContent(schedulerTask.ScheduledTaskName, schedulerTask.ResourceUri, "Failure", responseBody);
            }

            if (!string.IsNullOrWhiteSpace(emailContent.ToEmails))
            {
                this.emailService.SendEmail(emailContent);
            }

        }

        public bool CheckIfSchedulerExists(SchedulerUsageViewModel task, string planId,string resourceUri)
        {
            var tasks = this.schedulerRepository.GetAll().Where(s => s.ResourceUri == resourceUri
            && s.Frequency==task.SelectedSchedulerFrequency
            && s.Dimension==task.SelectedDimension
            && s.PlanId==planId
            && s.StartDate.Value == task.FirstRunDate.AddHours(task.TimezoneOffset)).ToList();

            return tasks.Any();
        
        }

        public void UpdateSchedulerResourceUri(string ResourceUri, string newResourceUri)
        {
            var tasks = this.schedulerRepository.GetAll().Where(s => s.ResourceUri == ResourceUri).ToList();
            foreach (var task in tasks)
            {
                task.ResourceUri = newResourceUri;
                this.schedulerRepository.Update(task);
            }
        }

        public string ProcessContainerMeterUsageResult(MeteredUsageResultModel meteredUsageResultModel)
        {
            // first update scheduler
            try
            {
                var scheduler = this.GetSchedulerByID(meteredUsageResultModel.ScheduledTaskId);

                if (meteredUsageResultModel.Status == "Accepted")
                {
                    scheduler.Status = SchedulerStatusEnum.Completed.ToString();
                }
                else
                {
                    scheduler.Status = SchedulerStatusEnum.Error.ToString();
                }
                this.UpdateScheduler(scheduler);
            }
            catch (Exception ex)
            {
                return $"Error during saving scheduler task:{ex.Message}";
            }
            return "OK";
        }
    }
}

