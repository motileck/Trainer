﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Trainer.Application.Aggregates.CSV.Commands.CSVToPatients;
using Trainer.Application.Aggregates.CSV.Queries.PatientsToCSV;
using Trainer.Application.Aggregates.Patient.Commands.CreatePatient;
using Trainer.Application.Aggregates.Patient.Commands.DeletePatient;
using Trainer.Application.Aggregates.Patient.Commands.UpdatePatient;
using Trainer.Application.Aggregates.Patient.Queries.GetPatient;
using Trainer.Application.Aggregates.Patient.Queries.GetPatients;
using Trainer.Application.Exceptions;
using Trainer.Common;
using Trainer.Enums;
using Trainer.Models;

namespace Trainer.Controllers
{
    public class PatientController : BaseController
    {
        private readonly IStringLocalizer<PatientController> Localizer;

        public PatientController(ILogger<PatientController> logger, IStringLocalizer<PatientController> localizer)
            : base(logger)
        {
            Localizer = localizer;
        }

        [HttpGet]
        [Authorize(Roles = "admin, doctor, manager")]
        public async Task<IActionResult> GetModels(
            SortState sortOrder = SortState.FirstNameSort,
            int? pageIndex = 1,
            int? pageSize = 10)
        {
            ViewData["FirstNameSort"] = sortOrder == SortState.FirstNameSort ? SortState.FirstNameSortDesc : SortState.FirstNameSort;
            ViewData["LastNameSort"] = sortOrder == SortState.LastNameSort ? SortState.LastNameSortDesc : SortState.LastNameSort;
            ViewData["MiddleNameSort"] = sortOrder == SortState.MiddleNameSort ? SortState.MiddleNameSortDesc : SortState.MiddleNameSort;
            ViewData["AgeSort"] = sortOrder == SortState.AgeSort ? SortState.AgeSortDesc : SortState.AgeSort;
            ViewData["SexSort"] = sortOrder == SortState.SexSort ? SortState.SexSortDesc : SortState.SexSort;

            var results = await Mediator.Send(new GetPatientsQuery (pageIndex, pageSize, sortOrder));
            return View(results);
        }

        [HttpGet]
        [Authorize(Roles = "admin, doctor, manager")]
        public async Task<IActionResult> GetModel(Guid id)
        {
            var patient = await Mediator.Send(new GetPatientQuery { PatientId = id });
            ViewBag.Results = patient.Results.Take(5);
            return View(patient);
        }

        [HttpGet]
        [Authorize(Roles = "admin, manager")]
        public IActionResult AddModel()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "admin, manager")]
        public async Task<IActionResult> AddModel(CreatePatientCommand command)
        {
            try
            {
                await Mediator.Send(command);
                return RedirectToAction("GetModels");
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError("All", ex.Errors.FirstOrDefault().Value);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var modelValue in ModelState.Values)
                {
                    modelValue.Errors.Clear();
                }
                foreach (var er in ex.Errors)
                {
                    ModelState.AddModelError(string.Empty, Localizer[er.ErrorMessage]);
                }
            }

            return View(command);
        }

        [HttpGet]
        [Authorize(Roles = "admin, manager")]
        public async Task<IActionResult> UpdateModel(Guid id)
        {
            var patient = await Mediator.Send(new GetPatientQuery { PatientId = id });
            ViewBag.Patient = patient;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "admin, manager")]
        public async Task<IActionResult> UpdateModel(UpdatePatientCommand command)
        {
            try
            {
                await Mediator.Send(command);
                return RedirectToAction("GetModels");
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError("All", ex.Errors.FirstOrDefault().Value);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var modelValue in ModelState.Values)
                {
                    modelValue.Errors.Clear();
                }

                foreach (var er in ex.Errors)
                {
                    ModelState.AddModelError(string.Empty, Localizer[er.ErrorMessage]);
                }
            }

            ViewBag.Patient = command;
            return View(command);
        }

        [Authorize(Roles = "admin, manager")]
        public async Task<RedirectToActionResult> DeleteModelAsync(Guid[] selectedPatient)
        {
            await Mediator.Send(new DeletePatientsCommand { PatientsId = selectedPatient });
            return RedirectToAction("GetModels");
        }

        [HttpGet]
        [Authorize(Roles = "admin, manager")]
        public async Task<IActionResult> ExportToCSV()
        {
            var fileInfo = await Mediator.Send(new PatientsToCSVQuery());
            return File(fileInfo.Content, fileInfo.Type.ToName(), fileInfo.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> ImportToCSV()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "admin, manager")]
        public async Task<IActionResult> ImportToCSV(CSV source)
        {
            await Mediator.Send(new CSVToPatientsCommand { CSVFile = source.File });
            return RedirectToAction("GetModels");
        }
    }
}
