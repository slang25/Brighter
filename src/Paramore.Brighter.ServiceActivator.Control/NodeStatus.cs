namespace Paramore.Brighter.ServiceActivator.Control;

public record NodeStatus
{
    /// <summary>
    /// The name of the node running Service Activator
    /// </summary>
    public string NodeName { get; init; }
    
    /// <summary>
    /// The Topics that this node can service
    /// </summary>
    public string[] AvailableTopics { get; init; }
    
    /// <summary>
    /// Information about currently configured subscriptions
    /// </summary>
    public NodeStatusSubscriptionInformation[] Subscriptions { get; init; }
    
    /// <summary>
    /// Is this node Healthy
    /// </summary>
    public bool IsHealthy { get => Subscriptions.Any(s => s.IsHealty != true); }
    
    /// <summary>
    /// The Number of Performers currently running on the Node
    /// </summary>
    public int NumberOfActivePerformers { get => Subscriptions.Sum(s => s.ActivePerformers); }
    
    /// <summary>
    /// Timestamp of Status Event
    /// </summary>
    public DateTime TimeStamp { get; } = DateTime.UtcNow;
    
    /// <summary>
    /// The version of the running process
    /// </summary>
    public string ExecutingAssemblyVersion { get; init; }
}
