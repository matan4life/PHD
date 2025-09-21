namespace Cdk.Configuration
{
    public class InfrastructureConfig
    {
        public string Environment { get; set; } = "dev";
        public string ProjectName { get; set; } = "fingerprint";
        public string StackName { get; set; } = "fingerprint";
        public string Region { get; set; } = "eu-central-1";
    }
}

