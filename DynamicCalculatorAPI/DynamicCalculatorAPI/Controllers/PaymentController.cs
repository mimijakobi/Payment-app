using DynamicCalculatorAPI.Interfaces;
using DynamicCalculatorAPI.Models;
using DynamicCalculatorAPI.Repository;
using DynamicExpresso;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IEngineService enginService;
    private readonly IJobService jobService;
    private readonly ICalculateService calculateService;
    public PaymentController(IEngineService enginService, IJobService jobService, ICalculateService calculateService )
    {
        this.enginService = enginService;
        this.jobService = jobService;
        this.calculateService = calculateService;
    }

    [HttpPost]
    public async Task<IActionResult> Run(int? limit = null)
    {
        var jobId = await jobService.CreateJobAsync();

        try
        {
            await enginService.RunAsync(jobId, limit); // ← מחכה עד הסוף
            return Ok(new { jobId, status = "completed" });
        }
        catch (Exception ex)
        {
            await jobService.UpdateStatusAsync(jobId, "failed", ex.Message);
            return StatusCode(500, ex.Message);
        }
    }
    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetStatus(Guid jobId)
    {
        var job = await jobService.GetStatusAsync(jobId);
        if (job == null)
            return NotFound();

        return Ok(new
        {
            status = job.Status
        });
    }

    [HttpGet("report/summary/{jobId}")]
    public async Task<IActionResult> GetSummary(Guid jobId)
    {
        var report = await calculateService.GetComparisonReportAsync(jobId);
        return Ok(report);
    }


}



