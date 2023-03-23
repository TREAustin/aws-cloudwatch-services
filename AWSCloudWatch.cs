using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;

namespace ace_conversion_csv.Services;

// For this class, the NuGet package AWSSDK.CloudWatchLogs needs to be added, make sure you don't just add AWSSDK.CloudWatch. 
// If the class is missing items still, only get the AWSSDK.<specific_package>.w
// Note - The AWSSDK.Core may cause ambiguity issues.
public class Logging
{
    //Instance of the AmazonCloudWatchLogs
    private IAmazonCloudWatchLogs client;
    //String used to store the LogGroupName, this is the name of folder.
    private string logGroup;
    //String used to store the LogStream, this is the name of the file.
    private string? logStream;

    //If more than one LogStream is being created during the application life cycle,
    //this will iterate to next available instance and not overwrite the current one.
    private string? nextSequenceToken;

    //This class follows a Factory Pattern and doesn't allow the constructor to be used.
    private Logging(string logGroup)
    {
        client = new AmazonCloudWatchLogsClient(Amazon.RegionEndpoint.USWest1);
        this.logGroup = logGroup;
    }
 
    /// <summary>
    /// Initializes an instance of the Logging class.
    /// This is the only method available for creating a new Logging instance.
    /// </summary>
    /// <returns>Instance of Logging class</returns>
    public static async Task<Logging> GetLoggerAsync(string logGroup)
    {
        var logger = new Logging(logGroup);
        await logger.SetupLogGroupAsync();
        await logger.CreateLogStreamAsync();
        return logger;
    }

    //Establishes the connection with the LogGroup in AWS CloudWatch.
    //If the LogGroup doesn't exist, it will be created.
    private async Task SetupLogGroupAsync()
    {
        var existingLogGroups = await client.DescribeLogGroupsAsync(new DescribeLogGroupsRequest()
        {
            LogGroupNamePrefix = logGroup
        });

        bool exists = existingLogGroups.LogGroups.Any(x => x.LogGroupName == logGroup);
        if (!exists)
        {
            _ = await client.CreateLogGroupAsync(new CreateLogGroupRequest()
            {
                LogGroupName = logGroup
            });
        }
    }
 
    //Creates the LogStream to be used when a log is store in AWS CloudWatch
    private async Task CreateLogStreamAsync()
    {
        logStream = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

        _ = await client.CreateLogStreamAsync(new CreateLogStreamRequest()
        {
            LogGroupName = logGroup,
            LogStreamName = logStream
        });
    }

    /// <summary>
    /// Method is to be used to store logs in CloudWatch.
    /// </summary>
    /// <returns>Task</returns>
    public async Task LogMessageAsync(string message)
    {
        var response = await client.PutLogEventsAsync(new PutLogEventsRequest()
        {
            LogGroupName = logGroup,
            LogStreamName = logStream,
            LogEvents = new List<InputLogEvent>()
            {
                new InputLogEvent()
                {
                    Message = message,
                    Timestamp = DateTime.UtcNow
                }
            }
        });
        nextSequenceToken = response.NextSequenceToken;
    }
}