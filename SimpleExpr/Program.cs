using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleExpr {
    public class TestContext : IExprContext {
        private Dictionary<string, ExprVar> _dict;

        public TestContext(Dictionary<string, ExprVar> dict) {
            _dict = dict;
        }

        public ExprVar Call(string name, List<ExprVar> parameters) {
            if (name == "add") {
                return parameters[0] + parameters[1];
            } else if (name == "test") {
                return parameters[0];
            } else if (name == "percent") {
                return new ExprVar($"{parameters[0].f * 100.0f}%");
            } else if (name == "minus") {
                return parameters[0] - parameters[1];
            }
            return new ExprVar(0f);
        }

        public ExprVar GetVar(string name) {
            if (_dict.TryGetValue(name, out var r)) {
                return r;
            }
            return new ExprVar(name);
        }
    }

    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Hello");
            UniTest("1+2", 3);
            UniTest("1+2*3", 7);
            UniTest("1*2+3", 5);
            UniTest("1*(2+3)", 5);
            UniTest("2*(2+3)", 10);
            UniTest("-2*(2+3)", -10);

            var vars = new Dictionary<string, SimpleExpr.ExprVar> {
                {"A", new SimpleExpr.ExprVar(2f) }, {"B", new SimpleExpr.ExprVar(3f) }
            };
            UniTestContext(" A", vars, 2);
            UniTestContext("A", vars, 2);
            UniTestContext("A+1", vars, 3);
            UniTestContext("1+A+3", vars, 6);
            UniTestContext("1--2", vars, 3);
            UniTestContext("A--A", vars, 4);
            UniTestContext("-2*(A+3)", vars, -10);
            UniTestContext("test(10)", vars, 10);
            UniTestContext("1+test(10)", vars, 11);
            UniTestContext("-test(10)", vars, -10);
            UniTestContext("A-test(A)", vars, 0);
            UniTestContext("add(A, A)", vars, 4);
            UniTestContext("-add(A, A)", vars, -4);
            UniTestContext("add(A, add(A, B))", vars, 7);
            UniTestContext("add(A, -add(A, B))", vars, -3);
            UniTestContext("add(A, A*2*add(A, B))", vars, 22);

            UniTestContextString("percent(0.02)", vars, "2%");
            UniTestContextString("percent(-0.02)", vars, "-2%");

            UniTestContextString("\"A\"", vars, "A");
            UniTestContextString("\"Hello World\"", vars, "Hello World");
            UniTestContextString("2+\"%\"", vars, "2%");
            UniTestContextString("add(2, \"%\")", vars, "2%");

            UniTestContextString("minus(10, 2) + \"%\"", vars, "8%");

            UniTestInvalid("?123", 0);
            UniTestInvalid("+123", 0);
            UniTestInvalid("++++123", 0);
            UniTestInvalid("1+2+3++123", 6);
            UniTestInvalid("A B", 1);

            Console.WriteLine("End");
        }

        private static void UniTest(string expr, float result) {
            var e = new ExprExecutor(expr);
            Debug.Assert(e.Eval() == new ExprVar(result));
        }

        private static void UniTestContext(string expr, Dictionary<string, ExprVar> dict, float result) {
            var e = new ExprExecutor(expr);
            var ctx = new TestContext(dict);
            Debug.Assert(e.Eval(ctx) == new ExprVar(result));
        }

        private static void UniTestContextString(string expr, Dictionary<string, ExprVar> dict, string result) {
            var e = new ExprExecutor(expr);
            var ctx = new TestContext(dict);
            var r = e.Eval(ctx);
            Debug.Assert(r == new ExprVar(result));
            Console.WriteLine(r);
        }

        private static void UniTestInvalid(string expr, int errorPos) {
            try {
                var e = new ExprExecutor(expr);
                e.Eval();
            } catch (InvalidExpressionException e) {
                Debug.Assert(e.errorPosition == errorPos);
            }
        }
    }
}
