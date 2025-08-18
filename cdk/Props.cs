namespace Cdk;

class Props
{
    public string DatasetBucketName = "app-dataset-bucket";
    public string DatasetMinutiaeDynamoTableName = "app-dataset-minutiae-table";
    public string InputBucketName = "app-input-bucket";
    public string InputMinutiaeDynamoTableName = "app-input-minutiae-table";
    public string ApplicationStackName = "application-stack";
    public string ExtractLambdaRoleName = "app-extract-lambda-role";
}
