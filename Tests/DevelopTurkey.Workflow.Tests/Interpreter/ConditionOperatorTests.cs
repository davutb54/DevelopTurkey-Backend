using Business.Models;
using DevelopTurkey.Workflow.Tests.Fixtures;

namespace DevelopTurkey.Workflow.Tests.Interpreter;

/// <summary>
/// Backend interpreter'ın condition operator implementasyonu testleri.
/// Backend SADECE eq/neq/gt/lt destekliyor (WorkflowInterpreterManager.cs:247-254).
/// Frontend ise 10 operator sunuyor (B1 bug — kanıtlanması bu test sınıfının amacı).
/// </summary>
public sealed class ConditionOperatorTests
{
    // ───────────────────────────────────────────────────────────────────────────
    // DESTEKLENEN OPERATÖRLER — eq, neq, gt, lt
    // ───────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Admin",       "Admin",       true)]
    [InlineData("Admin",       "User",        false)]
    [InlineData("admin",       "Admin",       true)]  // case-insensitive
    public async Task Eq_Operator_StringComparison(string actual, string expected, bool conditionMatches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserRole = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserRole", "eq", expected)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["msg"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["msg"] = "NO"  })
            .Build();

        var result = await harness.RunAsync(flow, ctx);

        result.Success.Should().BeTrue();
        result.Dispatched.Should().ContainSingle();
        var expectedMsg = conditionMatches ? "YES" : "NO";
        result.Dispatched[0].Parameters["msg"].Should().Be(expectedMsg);
    }

    [Theory]
    [InlineData(100, "100", true)]
    [InlineData(100, "50",  false)]
    public async Task Eq_Operator_NumberComparison(int actual, string expected, bool matches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserScore = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserScore", "eq", expected)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();

        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be(matches ? "YES" : "NO");
    }

    [Theory]
    [InlineData("Admin", "User",       true)]   // neq → true
    [InlineData("Admin", "Admin",      false)]  // neq → false
    public async Task Neq_Operator(string actual, string expected, bool matches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserRole = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserRole", "neq", expected)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be(matches ? "YES" : "NO");
    }

    [Theory]
    [InlineData(150, "100", true)]
    [InlineData(50,  "100", false)]
    [InlineData(100, "100", false)]  // strict greater
    public async Task Gt_Operator(int actual, string threshold, bool matches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserScore = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserScore", "gt", threshold)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be(matches ? "YES" : "NO");
    }

    [Theory]
    [InlineData(50,  "100", true)]
    [InlineData(150, "100", false)]
    [InlineData(100, "100", false)]  // strict less
    public async Task Lt_Operator(int actual, string threshold, bool matches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserScore = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserScore", "lt", threshold)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be(matches ? "YES" : "NO");
    }

    // ───────────────────────────────────────────────────────────────────────────
    // B1 — Ekstra operatörler (gte, lte, contains, startsWith, endsWith, in)
    // Önceden backend'de YOKTU; B1 fix ile WorkflowInterpreterManager.cs switch'e eklendi.
    // Bu testler artık DOĞRU davranışı doğrular (regression koruma).
    // ───────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(100, "50",  true)]   // 100 >= 50
    [InlineData(50,  "50",  true)]   // 50  >= 50 (boundary)
    [InlineData(49,  "50",  false)]  // 49  >= 50 (false)
    public async Task Gte_Operator(int actual, string value, bool matches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserScore = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserScore", "gte", value)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be(matches ? "YES" : "NO");
    }

    [Theory]
    [InlineData(50,  "100", true)]   // 50  <= 100
    [InlineData(100, "100", true)]   // 100 <= 100 (boundary)
    [InlineData(101, "100", false)]  // 101 <= 100 (false)
    public async Task Lte_Operator(int actual, string value, bool matches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserScore = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserScore", "lte", value)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be(matches ? "YES" : "NO");
    }

    [Theory]
    [InlineData("Admin",      "dmin",   true)]   // "Admin" contains "dmin"
    [InlineData("Admin",      "ADM",    true)]   // case-insensitive
    [InlineData("Admin",      "xyz",    false)]
    public async Task Contains_Operator(string actual, string value, bool matches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserRole = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserRole", "contains", value)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be(matches ? "YES" : "NO");
    }

    [Theory]
    [InlineData("Admin",      "Adm",    true)]
    [InlineData("Admin",      "adm",    true)]   // case-insensitive
    [InlineData("Admin",      "min",    false)]  // startsWith değil
    public async Task StartsWith_Operator(string actual, string value, bool matches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserRole = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserRole", "startsWith", value)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be(matches ? "YES" : "NO");
    }

    [Theory]
    [InlineData("Admin",      "min",    true)]
    [InlineData("Admin",      "MIN",    true)]   // case-insensitive
    [InlineData("Admin",      "Adm",    false)]
    public async Task EndsWith_Operator(string actual, string value, bool matches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserRole = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserRole", "endsWith", value)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be(matches ? "YES" : "NO");
    }

    [Theory]
    [InlineData("Admin",  "Admin,User",       true)]
    [InlineData("User",   "Admin,User",       true)]
    [InlineData("Guest",  "Admin,User",       false)]
    [InlineData("Admin",  " Admin , User ",   true)]   // whitespace tolerance
    public async Task In_Operator_Csv(string actual, string value, bool matches)
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserRole = actual };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserRole", "in", value)
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be(matches ? "YES" : "NO");
    }

    // ───────────────────────────────────────────────────────────────────────────
    // Edge cases
    // ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Unknown_Operator_Returns_False()
    {
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserRole = "Admin" };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserRole", "unknownOp", "Admin")
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be("NO");
    }

    [Fact]
    public async Task Null_Context_Field_Returns_False()
    {
        // B6 davranışı: alan null ise condition daima false.
        // AuthController.Login() context'inde UserRole set EDİLMİYOR;
        // "UserRole eq Admin" gibi user-defined kural daima NO branch'a düşer.
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { UserRole = null! };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("UserRole", "eq", "Admin")
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be("NO",
            "B6: publish noktasında alan set edilmediği için condition sessizce false döner");
    }

    [Fact]
    public async Task Metadata_Field_Resolution_Works()
    {
        // ResolveContextValue ayrıca Metadata dictionary'ye de bakar.
        var harness = new InterpreterHarness();
        var ctx = new RuleContext { Metadata = new Dictionary<string, object?> { ["IpAddress"] = "127.0.0.1" } };
        var flow = FlowJsonBuilder.Start()
            .Trigger()
            .Condition("IpAddress", "eq", "127.0.0.1")
            .Action("log_event", sourceHandle: "yes", fromNodeId: "cond-2", parameters: new() { ["b"] = "YES" })
            .Action("log_event", sourceHandle: "no",  fromNodeId: "cond-2", parameters: new() { ["b"] = "NO"  })
            .Build();
        var result = await harness.RunAsync(flow, ctx);
        result.Dispatched[0].Parameters["b"].Should().Be("YES");
    }
}
