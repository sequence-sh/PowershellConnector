using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Reductech.EDR.Connectors.Pwsh.Tests
{
    public class PwshRunnerTests
    {
        [Fact]
        public void ProcessData_WhenSenderIsNotPSDataCollection_Throws()
        {
            var sender = new PSDataCollection<object>();

            Assert.Throws<ArgumentException>(() =>
                PwshRunner.ProcessData<PSObject>(sender, 0, o => o.ToString()));
        }
        
        [Fact]
        public void ProcessData_WhenSenderIsPSDataCollection_RemovesItemFromCollection()
        {
            var sender = new PSDataCollection<string>();
            var expected = "string0";
            sender.Add(expected);
            sender.Add("string1");

            PwshRunner.ProcessData<string>(sender, 0, o => { });
            
            Assert.Single(sender);
            Assert.DoesNotContain(sender, o => expected.Equals(o));
        }
        
        [Fact]
        public void ProcessData_WhenSenderIsPSDataCollection_InvokesAction()
        {
            var sender = new PSDataCollection<object>();
            var str = "";
            sender.Add(str);
            var mock = new Mock<Action<object>>();
            
            PwshRunner.ProcessData<object>(sender, 0, mock.Object);
            
            mock.Verify(m => m.Invoke(str));
        }
        
    }
}
