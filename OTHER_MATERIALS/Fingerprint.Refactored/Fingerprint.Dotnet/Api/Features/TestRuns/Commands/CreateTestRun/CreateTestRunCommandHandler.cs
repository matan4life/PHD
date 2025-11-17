using Api.Database;
using Api.Entities;
using Api.Models;
using MediatR;

namespace Api.Features.TestRuns.Commands.CreateTestRun;

public class CreateTestRunCommandHandler(FingerprintContext context)
    : IRequestHandler<CreateTestRunCommand, CreateTestRunResponse>
{
    private const string DatasetsRootFolder = @"E:\PHD\Fingerprint.Refactored\fs\datasets";
    
    public async Task<CreateTestRunResponse> Handle(CreateTestRunCommand request, CancellationToken cancellationToken)
    {
        var dateTimeNow = DateTime.Now;
        var testRun = new TestRun
        {
            DatasetPath = $@"{DatasetsRootFolder}\{request.DatasetName}",
            StartDate = DateTime.UtcNow
        };
        
        await context.TestRuns.AddAsync(testRun, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        var endDateTime = DateTime.Now;
        return new CreateTestRunResponse(new TelemetryResponse(dateTimeNow, endDateTime, endDateTime - dateTimeNow), testRun.Id);
    }
}