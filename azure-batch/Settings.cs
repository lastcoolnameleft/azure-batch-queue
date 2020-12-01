namespace Microsoft.Azure.Batch.Samples.HelloWorld
{
    using System.Text;
    using Common;

    public partial class Settings
    {
        public string PoolId { get; set; }
        public int PoolTargetNodeCount { get; set; }
        public string PoolOSFamily { get; set; }
        public string PoolNodeVirtualMachineSize { get; set; }
        public bool JobKeepAlive { get; set; }
        public int TaskCount { get; set; }
        public string TaskCommand { get; set; }
        public string ApplicationId{ get; set; }
        public string ApplicationVersion { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public int TaskSlotsPerNode { get; set; }
        public bool PoolKeepAlive { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            SampleHelpers.AddSetting(stringBuilder, "PoolId", this.PoolId);
            SampleHelpers.AddSetting(stringBuilder, "PoolTargetNodeCount", this.PoolTargetNodeCount);
            SampleHelpers.AddSetting(stringBuilder, "PoolOSFamily", this.PoolOSFamily);
            SampleHelpers.AddSetting(stringBuilder, "PoolNodeVirtualMachineSize", this.PoolNodeVirtualMachineSize);
            SampleHelpers.AddSetting(stringBuilder, "JobKeepAlive", this.JobKeepAlive);
            SampleHelpers.AddSetting(stringBuilder, "TaskCount", this.TaskCount);
            SampleHelpers.AddSetting(stringBuilder, "TaskCommand", this.TaskCommand);
            SampleHelpers.AddSetting(stringBuilder, "ApplicationId", this.ApplicationId);
            SampleHelpers.AddSetting(stringBuilder, "ApplicationVersion", this.ApplicationVersion);
            SampleHelpers.AddSetting(stringBuilder, "MaxDegreeOfParallelism", this.MaxDegreeOfParallelism);
            SampleHelpers.AddSetting(stringBuilder, "TaskSlotsPerNode", this.TaskSlotsPerNode);
            SampleHelpers.AddSetting(stringBuilder, "PoolKeepAlive", this.PoolKeepAlive);

            return stringBuilder.ToString();
        }
    }
}
