// See https://aka.ms/new-console-template for more information
using Amazon.CDK;
using Cdk;

var app = new App();
new ApplicationStack(app, new Props());
app.Synth();