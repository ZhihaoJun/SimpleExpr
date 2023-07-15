using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleExpr {
    public struct ExprVar {
        public enum VarType {
            Number,
            String
        }

        public VarType type;
        public string str;
        public float f;

        public ExprVar(string s) {
            type = VarType.String;
            str = s;
            f = 0;
        }

        public ExprVar(float f) {
            type = VarType.Number;
            str = null;
            this.f = f;
        }

        public static ExprVar operator +(ExprVar lhs, ExprVar rhs) {
            if (lhs.type == VarType.Number && rhs.type == VarType.Number) {
                return new ExprVar(lhs.f + rhs.f);
            }
            else if (lhs.type == VarType.Number && rhs.type == VarType.String) {
                return new ExprVar(lhs.f.ToString() + rhs.str);
            }
            else if (lhs.type == VarType.String && rhs.type == VarType.Number) {
                return new ExprVar(lhs.str + rhs.f.ToString());
            }
            else if (lhs.type == VarType.String && rhs.type == VarType.String) {
                return new ExprVar(lhs.str + rhs.str);
            }

            return new ExprVar("");
        }

        public static ExprVar operator -(ExprVar lhs, ExprVar rhs) {
            if (lhs.type == VarType.Number && rhs.type == VarType.Number) {
                return new ExprVar(lhs.f - rhs.f);
            }

            return new ExprVar(0f);
        }

        public static ExprVar operator -(ExprVar rhs) {
            if (rhs.type == VarType.Number) {
                return new ExprVar(-rhs.f);
            }

            return new ExprVar(0f);
        }

        public static ExprVar operator *(ExprVar lhs, ExprVar rhs) {
            if (lhs.type == VarType.Number && rhs.type == VarType.Number) {
                return new ExprVar(lhs.f * rhs.f);
            }

            return new ExprVar(0f);
        }

        public static ExprVar operator /(ExprVar lhs, ExprVar rhs) {
            if (lhs.type == VarType.Number && rhs.type == VarType.Number) {
                return new ExprVar(lhs.f / rhs.f);
            }

            return new ExprVar(0f);
        }

        public static bool operator ==(ExprVar lhs, ExprVar rhs) {
            if (lhs.type == VarType.Number && rhs.type == VarType.Number) {
                return lhs.f == rhs.f;
            }
            else if (lhs.type == VarType.Number && rhs.type == VarType.String) {
                return lhs.f.ToString() == rhs.str;
            }
            else if (lhs.type == VarType.String && rhs.type == VarType.Number) {
                return lhs.str == rhs.f.ToString();
            }
            else if (lhs.type == VarType.String && rhs.type == VarType.String) {
                return lhs.str == rhs.str;
            }

            return false;
        }

        public static bool operator !=(ExprVar lhs, ExprVar rhs) {
            return !(lhs == rhs);
        }

        public override string ToString() {
            switch (type) {
                case VarType.Number:
                    return f.ToString();
                case VarType.String:
                    return str;
            }

            return "";
        }

        public string ToString(string format) {
            switch (type) {
                case VarType.Number:
                    return f.ToString(format);
                case VarType.String:
                    return str;
            }

            return "";
        }
    }

    public interface IExprContext {
        public ExprVar GetVar(string name);
        public ExprVar Call(string name, List<ExprVar> parameters);
    }

    public class InvalidExpressionException : Exception {
        public string expression;
        public int errorPosition;

        public InvalidExpressionException(string expression, int errorPosition) :
            base($"Invalid expression, {expression.Substring(errorPosition)} (at pos {errorPosition})") {
            this.expression = expression;
            this.errorPosition = errorPosition;
        }

        public InvalidExpressionException(string expression, int errorPosition, Exception e) :
            base($"Invalid expression, {expression.Substring(errorPosition)} (at pos {errorPosition})", e) {
            this.expression = expression;
            this.errorPosition = errorPosition;
        }
    }


    class ExprExecutor {
        private enum TokenType {
            Invalid,
            Identifier,
            Number,
            Add,
            Minus,
            Mul,
            Div,
            LBracket,
            RBracket,
            Comma,
            String
        }

        public enum OperatorType {
            OpAdd,
            OpMinus,
            OpNegative,
            OpMul,
            OpDiv,
            OpCall,
            OpLBraket
        }

        public enum OperandType {
            Static
        }

        private struct Operator {
            public OperatorType type;
            public string fname;
            public int paramCount;

            public int priority => ExprExecutor.opPriority[type];
        }

        private struct Operand {
            public OperandType type;
            public SimpleExpr.ExprVar var;
        }

        private static Regex floatRegex = new Regex(@"(-?[1-9]\d*\.\d+|-?0\.\d+|-?[1-9]\d*|0|-?\.\d+|-?\d+\.)");
        public static Regex identifierRegex = new Regex(@"([a-zA-Z_$][a-zA-Z_0-9$]*)");

        private static Dictionary<OperatorType, int> opPriority = new Dictionary<OperatorType, int> {
            { OperatorType.OpAdd, 5 }, { OperatorType.OpMinus, 5 }, { OperatorType.OpNegative, 2 },
            { OperatorType.OpMul, 4 }, { OperatorType.OpDiv, 4 }, { OperatorType.OpCall, 1 },
            { OperatorType.OpLBraket, 100 }
        };

        private string expr;
        private int currentOffset;
        private SimpleExpr.IExprContext context;
        private Stack<Operator> operators = new Stack<Operator>();
        private Stack<Operand> operands = new Stack<Operand>();
        private TokenType lastToken;

        public ExprExecutor(string expr) {
            this.expr = expr;
            this.currentOffset = 0;
        }

        public SimpleExpr.ExprVar Eval(SimpleExpr.IExprContext context = null) {
            this.context = context;
            try {
                return LExpr(0);
            }
            catch (Exception e) {
                throw new InvalidExpressionException(expr, currentOffset, e);
            }
        }

        private SimpleExpr.ExprVar LExpr(int opStackBorder) {
            while (true) {
                var token = "";
                var length = 0;
                if (currentOffset >= expr.Length) {
                    break;
                }

                var curToken = ParseToken(currentOffset, out token, out length);

                if (curToken == TokenType.Invalid) {
                    throw new InvalidExpressionException(expr, currentOffset);
                }

                switch (curToken) {
                    case TokenType.Identifier:
                        if (lastToken == TokenType.Identifier) {
                            throw new InvalidExpressionException(expr, currentOffset);
                        }

                        if (ParseToken(currentOffset + length, out _, out var lbraLength) == TokenType.LBracket) {
                            var paramCount = 0;
                            currentOffset += length + lbraLength;
                            while (true) {
                                var param = LExpr(opStackBorder + operators.Count);
                                operands.Push(new Operand {
                                    type = OperandType.Static,
                                    var = param
                                });

                                paramCount++;

                                var ntoken = ParseToken(currentOffset, out _, out var nTokenLength);
                                if (ntoken == TokenType.Comma) {
                                    lastToken = TokenType.Comma;
                                    currentOffset += nTokenLength;
                                }
                                else if (ntoken == TokenType.RBracket) {
                                    currentOffset += nTokenLength;
                                    break;
                                }
                            }

                            operators.Push(new Operator {
                                type = OperatorType.OpCall,
                                fname = token,
                                paramCount = paramCount
                            });
                        }
                        else {
                            operands.Push(new Operand {
                                type = OperandType.Static,
                                var = context?.GetVar(token) ?? new SimpleExpr.ExprVar(0f)
                            });
                            currentOffset += length;
                        }

                        break;
                    case TokenType.Number:
                        var num = float.Parse(token);
                        operands.Push(new Operand {
                            type = OperandType.Static,
                            var = new SimpleExpr.ExprVar(num)
                        });
                        currentOffset += length;
                        break;
                    case TokenType.Add: {
                        if (lastToken == TokenType.Invalid) {
                            throw new InvalidExpressionException(expr, currentOffset);
                        }

                        var op = new Operator {
                            type = OperatorType.OpAdd
                        };
                        CheckOperatorPriority(op);
                        operators.Push(op);
                        currentOffset += length;
                        break;
                    }
                    case TokenType.Minus: {
                        var op = new Operator {
                            type = OperatorType.OpMinus
                        };
                        if (lastToken != TokenType.Number && lastToken != TokenType.Identifier) {
                            var ahead = ParseToken(currentOffset + length, out _, out _);
                            if (ahead == TokenType.Number ||
                                ahead == TokenType.Identifier) {
                                op.type = OperatorType.OpNegative;
                            }
                            else {
                                if (lastToken == TokenType.Invalid) {
                                    throw new InvalidExpressionException(expr, currentOffset);
                                }
                            }
                        }

                        CheckOperatorPriority(op);
                        operators.Push(op);
                        currentOffset += length;
                        break;
                    }
                    case TokenType.Mul: {
                        if (lastToken == TokenType.Invalid) {
                            throw new InvalidExpressionException(expr, currentOffset);
                        }

                        var op = new Operator {
                            type = OperatorType.OpMul
                        };
                        CheckOperatorPriority(op);
                        operators.Push(op);
                        currentOffset += length;
                        break;
                    }
                    case TokenType.Div: {
                        if (lastToken == TokenType.Invalid) {
                            throw new InvalidExpressionException(expr, currentOffset);
                        }

                        var op = new Operator {
                            type = OperatorType.OpDiv
                        };
                        CheckOperatorPriority(op);
                        operators.Push(op);
                        currentOffset += length;
                        break;
                    }
                    case TokenType.LBracket:
                        operators.Push(new Operator {
                            type = OperatorType.OpLBraket
                        });
                        currentOffset += length;
                        break;
                    case TokenType.RBracket:
                        if (!FindLBraket(opStackBorder)) {
                            goto end;
                        }

                        while (true) {
                            if (operators.Peek().type == OperatorType.OpLBraket) {
                                operators.Pop();
                                break;
                            }

                            var op = operators.Pop();
                            DoOperator(op);
                        }

                        currentOffset += length;
                        break;
                    case TokenType.String:
                        operands.Push(new Operand {
                            type = OperandType.Static,
                            var = new SimpleExpr.ExprVar(token)
                        });
                        currentOffset += length;
                        break;
                    case TokenType.Comma:
                        goto end;
                }

                lastToken = curToken;
            }

            end:
            // Clear operator stack
            while (true) {
                if (operators.Count <= opStackBorder) {
                    break;
                }

                var op = operators.Pop();
                DoOperator(op);
            }

            return operands.Pop().var;
        }

        private void LastTokenAssert(params TokenType[] types) {
            for (var i = 0; i < types.Length; i++) {
                if (lastToken == types[i]) {
                    return;
                }
            }

            throw new InvalidExpressionException(expr, currentOffset);
        }

        private bool FindLBraket(int opBorder) {
            var index = 0;
            foreach (var op in operators) {
                if (index >= opBorder && op.type == OperatorType.OpLBraket) {
                    return true;
                }

                index++;
            }

            return false;
        }

        private void CheckOperatorPriority(Operator op) {
            if (operators.Count == 0) {
                return;
            }

            var top = operators.Peek();
            if (top.priority <= op.priority) {
                operators.Pop();
                DoOperator(top);
            }
        }

        private void DoOperator(Operator op) {
            switch (op.type) {
                case OperatorType.OpAdd: {
                    var rhs = operands.Pop();
                    var lhs = operands.Pop();
                    PushNumber(lhs.var + rhs.var);
                    break;
                }
                case OperatorType.OpMinus: {
                    var rhs = operands.Pop();
                    var lhs = operands.Pop();
                    PushNumber(lhs.var - rhs.var);
                    break;
                }
                case OperatorType.OpNegative: {
                    var rhs = operands.Pop();
                    PushNumber(-rhs.var);
                    break;
                }
                case OperatorType.OpMul: {
                    var rhs = operands.Pop();
                    var lhs = operands.Pop();
                    PushNumber(lhs.var * rhs.var);
                    break;
                }
                case OperatorType.OpDiv: {
                    var rhs = operands.Pop();
                    var lhs = operands.Pop();
                    PushNumber(lhs.var / rhs.var);
                    break;
                }
                case OperatorType.OpCall: {
                    var paramList = new List<SimpleExpr.ExprVar>();
                    for (var i = 0; i < op.paramCount; i++) {
                        var n = operands.Pop();
                        paramList.Add(n.var);
                    }

                    paramList.Reverse();
                    var r = context.Call(op.fname, paramList);
                    PushNumber(r);
                    break;
                }
            }
        }

        private void PushNumber(SimpleExpr.ExprVar v) {
            operands.Push(new Operand {
                type = OperandType.Static,
                var = v
            });
        }

        private TokenType ParseToken(int offset, out string token, out int length) {
            token = null;
            length = 0;
            if (offset >= expr.Length) {
                return TokenType.Invalid;
            }

            var realOffset = offset;
            // Skip whitespace
            while (true) {
                if (realOffset >= expr.Length || expr[realOffset] != ' ') {
                    break;
                }

                realOffset++;
            }

            var whiteSkipped = realOffset - offset;
            if (expr[realOffset] == '+') {
                length = 1 + whiteSkipped;
                return TokenType.Add;
            }
            else if (expr[realOffset] == '-') {
                length = 1 + whiteSkipped;
                return TokenType.Minus;
            }
            else if (expr[realOffset] == '*') {
                length = 1 + whiteSkipped;
                return TokenType.Mul;
            }
            else if (expr[realOffset] == '/') {
                length = 1 + whiteSkipped;
                return TokenType.Div;
            }
            else if (expr[realOffset] == '(') {
                length = 1 + whiteSkipped;
                return TokenType.LBracket;
            }
            else if (expr[realOffset] == ')') {
                length = 1 + whiteSkipped;
                return TokenType.RBracket;
            }
            else if (expr[realOffset] == ',') {
                length = 1 + whiteSkipped;
                return TokenType.Comma;
            }

            if (expr[realOffset] == '"') {
                var i = 1;
                while (true) {
                    if (expr[realOffset + i] == '"') {
                        break;
                    }

                    i++;
                }

                token = expr.Substring(realOffset + 1, i - 1);
                length = i + 1 + whiteSkipped;
                return TokenType.String;
            }

            var r = identifierRegex.Match(expr, realOffset);
            if (r.Success && r.Index == realOffset) {
                token = r.Groups[0].Value;
                length = r.Groups[0].Length + whiteSkipped;
                return TokenType.Identifier;
            }

            r = floatRegex.Match(expr, realOffset);
            if (r.Success && r.Index == realOffset) {
                token = r.Groups[0].Value;
                length = r.Groups[0].Length + whiteSkipped;
                return TokenType.Number;
            }

            return TokenType.Invalid;
        }
    }
}