using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using CSharpFunctionalExtensions;
using Moq;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.TestHarness;
using Xunit;
using Entity = Reductech.EDR.Core.Entity;

namespace Reductech.EDR.Connectors.Pwsh.Tests
{

public class PwshRunnerTests
{
    [Fact]
    public void ProcessData_WhenSenderIsNotPSDataCollection_Throws()
    {
        var sender = new PSDataCollection<object>();

        Assert.Throws<ArgumentException>(
            () =>
                PwshRunner.ProcessData<PSObject>(sender, 0, _ => { })
        );
    }

    [Fact]
    public void ProcessData_WhenSenderIsPSDataCollection_RemovesItemFromCollection()
    {
        var sender   = new PSDataCollection<string>();
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
        var str    = "";
        var mock   = new Mock<Action<object>>();

        sender.Add(str);

        PwshRunner.ProcessData<object>(sender, 0, mock.Object);

        mock.Verify(m => m.Invoke(str));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScript_ReadsDataFromOutputStream()
    {
        var logger = new TestLogger();
        var script = @"Write-Output 'one'; Write-Output 2";

        var result = await PwshRunner.RunScript(script, logger).ToListAsync();

        Assert.Equal(new List<PSObject> { "one", 2 }, result);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void RunScript_LogsErrorsAndWarnings()
    {
        var logger = new TestLogger();

        var script =
            @"Write-Output 'one'; Write-Error 'error'; Write-Output 'two'; Write-Warning 'warning'";

        _ = await PwshRunner.RunScript(script, logger).ToListAsync();

        Assert.Equal(2, logger.LoggedValues.Count);
        Assert.Contains(logger.LoggedValues, o => o.Equals("error"));
        Assert.Contains(logger.LoggedValues, o => o.Equals("warning"));
    }

    [Fact]
    public void EntityFromPSObject_WhenPSObjectIsNull_Throws()
    {
        var err = Assert.Throws<NullReferenceException>(() => PwshRunner.EntityFromPSObject(null!));
        Assert.Matches("cannot be null", err.Message);
    }

    [Fact]
    public void EntityFromPSObject_WhenBaseObjectIsPSO_ReturnsMultiValueEntity()
    {
        var pso = new PSObject();

        pso.Properties.Add(new PSNoteProperty("prop1", "value1"));
        pso.Properties.Add(new PSNoteProperty("prop2", "value2"));

        var entity = PwshRunner.EntityFromPSObject(pso);

        Assert.NotNull(entity);

        var val1 = entity.TryGetValue("prop1").Map(x => x.GetString());
        var val2 = entity.TryGetValue("prop2").Map(x => x.GetString());

        Assert.Equal(2,        entity.Count());
        Assert.Equal("value1", val1!.ToString());
        Assert.Equal("value2", val2!.ToString());
    }

    [Theory]
    [InlineData("hello")]
    [InlineData(123)]
    [InlineData(1.5)]
    [InlineData(true)]
    public void EntityFromPSObject_WhenBaseObjectIsNotPSO_ReturnsSingleValueEntity(object expected)
    {
        var pso = new PSObject(expected);

        var entity = PwshRunner.EntityFromPSObject(pso);

        Assert.NotNull(entity);

        var val = entity.TryGetValue(Entity.PrimitiveKey).Value;

        Assert.Equal(expected, val.Value.Value);
    }

    [Fact]
    public void EntityFromPSObject_WhenBaseObjectIsHashtable_ReturnsEntity()
    {
        var pso = new PSObject(new Hashtable { { "prop1", "value1" }, { "prop2", 2 } });

        var entity = PwshRunner.EntityFromPSObject(pso);

        Assert.NotNull(entity);

        var val1 = entity.TryGetValue("prop1").Map(x => x.GetString());
        var val2 = entity.TryGetValue("prop2").Bind(x => x.TryGetInt());

        Assert.Equal(2,        entity.Count());
        Assert.Equal("value1", val1);
        Assert.Equal(2,        val2);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void EntityFromPSObject_WhenBaseObjectIsHashtable_ReturnsEntity_Integration()
    {
        var logger = new TestLogger();
        var script = @"@{prop1 = 'value1'; prop2 = 2} | Write-Output";

        var result = await PwshRunner.GetEntityEnumerable(script, logger).ToListAsync();

        Assert.Single(result);

        var val1 = result[0].TryGetValue("prop1").Map(x => x.GetString());
        var val2 = result[0].TryGetValue("prop2").Bind(x => x.TryGetInt());

        Assert.Equal("value1", val1);
        Assert.Equal(2,        val2);
    }

    [Fact]
    public void EntityFromPSObject_WhenBaseObjectIsArray_ReturnsEntity()
    {
        var arr = new object[] { "value1", 2 };
        var pso = new PSObject(arr);

        var entity = PwshRunner.EntityFromPSObject(pso);

        Assert.NotNull(entity);

        var actual = entity.TryGetValue(Entity.PrimitiveKey).Bind(x => x.TryGetEntityValueList());

        Assert.Equal(arr[0], actual.Value[0].ToString());
        Assert.Equal(arr[1], actual.Value[1].TryGetInt().Value);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async void EntityFromPSObject_WhenBaseObjectIsArray_ReturnsEntity_Integration()
    {
        var logger = new TestLogger();
        var script = @"Write-Output -NoEnumerate @('value1', 2)";

        var result = await PwshRunner.GetEntityEnumerable(script, logger).ToListAsync();

        Assert.Single(result);

        var actual =
            result[0].TryGetValue(Entity.PrimitiveKey).Bind(x => x.TryGetEntityValueList());

        Assert.Equal("value1", actual.Value[0].ToString());
        Assert.Equal(2,        actual.Value[1].TryGetInt().Value);
    }
}

}
