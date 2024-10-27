using System.Text.RegularExpressions;
using Amazon.Lambda.DynamoDBEvents;

namespace Oproto.FluentDynamoDb.Streams;

public class DynamoDbStreamRecordProcessor
{
    public DynamoDBEvent.DynamodbStreamRecord Record { get; private init; }

    public DynamoDbStreamRecordProcessor(DynamoDBEvent.DynamodbStreamRecord record)
    {
        Record = record;
    }
    
    public async Task<DynamoDbStreamRecordProcessor> Awaitable()
    {
        return await Task.Run(() => this);
    }
}

public static class DynamoDbStreamProcessorExtensions
{
    private static async Task InternalProcess(this Task<DynamoDbStreamRecordProcessor> recordProcessor, Func<Task<DynamoDbStreamRecordEventProcessor>,Task> processFunc)
    {
        DynamoDbStreamRecordEventProcessor eventProcessor = new() { Record = recordProcessor.Result.Record };
        await processFunc(eventProcessor.Awaitable());
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="recordProcessor"></param>
    /// <param name="pkName"></param>
    /// <param name="pkValue"></param>
    /// <param name="processFunc"></param>
    /// <returns></returns>
    public static async Task<DynamoDbStreamRecordProcessor> OnMatch(
        this Task<DynamoDbStreamRecordProcessor> recordProcessor, string pkName, string pkValue, Func<Task<DynamoDbStreamRecordEventProcessor>,Task> processFunc)
    {
        await recordProcessor;
        if (recordProcessor.Result.Record.Dynamodb.Keys[pkName].S==pkValue)
        {
            await InternalProcess(recordProcessor, processFunc);
        }
        return recordProcessor.Result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="recordProcessor"></param>
    /// <param name="pkName"></param>
    /// <param name="pkValue"></param>
    /// <param name="skName"></param>
    /// <param name="skValue"></param>
    /// <param name="processFunc"></param>
    /// <returns></returns>
    public static async Task<DynamoDbStreamRecordProcessor> OnMatch(
        this Task<DynamoDbStreamRecordProcessor> recordProcessor,
        string pkName, string pkValue, string skName, string skValue, Func<Task<DynamoDbStreamRecordEventProcessor>, Task> processFunc)
    {
        await recordProcessor;
        if (recordProcessor.Result.Record.Dynamodb.Keys[pkName].S==pkValue && recordProcessor.Result.Record.Dynamodb.Keys[skName].S==skValue)
        {
            await InternalProcess(recordProcessor, processFunc);
        }
        return recordProcessor.Result;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="recordProcessor"></param>
    /// <param name="skName"></param>
    /// <param name="skValue"></param>
    /// <param name="processFunc"></param>
    /// <returns></returns>
    public static async Task<DynamoDbStreamRecordProcessor> OnSortKeyMatch(
        this Task<DynamoDbStreamRecordProcessor> recordProcessor,
        string skName, string skValue, Func<Task<DynamoDbStreamRecordEventProcessor>, Task> processFunc)
    {
        await recordProcessor;
        if (recordProcessor.Result.Record.Dynamodb.Keys[skName].S==skValue)
        {
            await InternalProcess(recordProcessor, processFunc);
        }
        return recordProcessor.Result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="recordProcessor"></param>
    /// <param name="pkName"></param>
    /// <param name="pkValue"></param>
    /// <param name="skName"></param>
    /// <param name="skPattern"></param>
    /// <param name="processFunc"></param>
    /// <returns></returns>
    public static async Task<DynamoDbStreamRecordProcessor> OnPatternMatch(
        this Task<DynamoDbStreamRecordProcessor> recordProcessor,
        string pkName, string pkValue, string skName, Regex skPattern, Func<Task<DynamoDbStreamRecordEventProcessor>,Task> processFunc)
    {
        if (recordProcessor.Result.Record.Dynamodb.Keys[pkName].S==pkValue
            && skPattern.IsMatch(recordProcessor.Result.Record.Dynamodb.Keys[skName].S))
        {
            await InternalProcess(recordProcessor, processFunc);
        }
        return recordProcessor.Result;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="recordProcessor"></param>
    /// <param name="skName"></param>
    /// <param name="skPattern"></param>
    /// <param name="processFunc"></param>
    /// <returns></returns>
    public static async Task<DynamoDbStreamRecordProcessor> OnSortKeyPatternMatch(
        this Task<DynamoDbStreamRecordProcessor> recordProcessor,
        string skName, Regex skPattern, Func<Task<DynamoDbStreamRecordEventProcessor>,Task> processFunc)
    {
        if (skPattern.IsMatch(recordProcessor.Result.Record.Dynamodb.Keys[skName].S))
        {
            await InternalProcess(recordProcessor, processFunc);
        }
        return recordProcessor.Result;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="recordProcessor"></param>
    /// <param name="pkName"></param>
    /// <param name="pkPattern"></param>
    /// <param name="processFunc"></param>
    /// <returns></returns>
    public static async Task<DynamoDbStreamRecordProcessor> OnPatternMatch(
        this Task<DynamoDbStreamRecordProcessor> recordProcessor,
        string pkName, Regex pkPattern, Func<Task<DynamoDbStreamRecordEventProcessor>,Task> processFunc)
    {
        if (pkPattern.IsMatch(recordProcessor.Result.Record.Dynamodb.Keys[pkName].S))
        {
            await InternalProcess(recordProcessor, processFunc);
        }
        return recordProcessor.Result;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="recordProcessor"></param>
    /// <param name="pkName"></param>
    /// <param name="pkPattern"></param>
    /// <param name="skName"></param>
    /// <param name="skPattern"></param>
    /// <param name="processFunc"></param>
    /// <returns></returns>
    public static async Task<DynamoDbStreamRecordProcessor> OnPatternMatch(
        this Task<DynamoDbStreamRecordProcessor> recordProcessor,
        string pkName, Regex pkPattern, string skName, Regex skPattern,
        Func<Task<DynamoDbStreamRecordEventProcessor>,Task> processFunc)
    {
        if (pkPattern.IsMatch(recordProcessor.Result.Record.Dynamodb.Keys[pkName].S) && skPattern.IsMatch(recordProcessor.Result.Record.Dynamodb.Keys[skName].S))
        {
            await InternalProcess(recordProcessor, processFunc);
        }
        return recordProcessor.Result;
    }
}