using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bench
{
    public static class ExpressionEvaluator
    {
        // ---------- Token types ----------
        private enum TokenType
        {
            Number,
            Plus,
            Minus,
            Multiply,
            Divide,
            Power,
            LeftParen,
            RightParen,
            UnaryPlus,
            UnaryMinus
        }

        // ---------- Token ----------
        private class Token
        {
            public TokenType Type;
            public double Value;   // for numbers
            public char Op;        // for operators (ignored for numbers)

            public Token(TokenType type, double value = 0, char op = '\0')
            {
                Type = type;
                Value = value;
                Op = op;
            }
        }

        // ---------- Public API ----------
        public static double Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new FormatException("Empty expression");

            List<Token> tokens = Tokenize(expression);
            if (tokens.Count == 0)
                throw new FormatException("Empty expression");

            List<object> rpn = ShuntingYard(tokens);
            return EvaluateRPN(rpn);
        }

        // ---------- Tokenization ----------
        private static List<Token> Tokenize(string expr)
        {
            List<Token> tokens = new List<Token>();
            int n = expr.Length;
            int i = 0;

            while (i < n)
            {
                char c = expr[i];

                // skip whitespace
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                // number (including optional leading sign handled later)
                if (char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    bool hasDot = false;
                    while (i < n && (char.IsDigit(expr[i]) || expr[i] == '.'))
                    {
                        if (expr[i] == '.')
                        {
                            if (hasDot) break;
                            hasDot = true;
                        }
                        i++;
                    }
                    string numStr = expr.Substring(start, i - start);
                    int dotCount = numStr.Count(ch => ch == '.');
                    if (dotCount > 1)
                        throw new FormatException("Invalid number format");

                    double value = double.Parse(numStr, CultureInfo.InvariantCulture);
                    tokens.Add(new Token(TokenType.Number, value));
                    continue;
                }

                // parentheses
                if (c == '(' || c == ')')
                {
                    tokens.Add(new Token(TokenType.LeftParen, 0));
                    i++;
                    continue;
                }

                // binary or unary + / -
                if (c == '+' || c == '-')
                {
                    bool isUnary = (tokens.Count == 0) ||
                                   IsUnaryPrev(tokens);

                    // if the sign is followed by a digit or '.', treat it as part of a number
                    if (isUnary && i + 1 < n &&
                        (char.IsDigit(expr[i + 1]) || expr[i + 1] == '.'))
                    {
                        int start = i;
                        char sign = c;
                        i++; // skip the sign
                        bool hasDot = false;
                        while (i < n && (char.IsDigit(expr[i]) || expr[i] == '.'))
                        {
                            if (expr[i] == '.')
                            {
                                if (hasDot) break;
                                hasDot = true;
                            }
                            i++;
                        }
                        string numStr = expr.Substring(start, i - start);
                        int dotCount = numStr.Count(ch => ch == '.');
                        if (dotCount > 1)
                            throw new FormatException("Invalid number");
                        double value = double.Parse(numStr, CultureInfo.InvariantCulture);
                        tokens.Add(new Token(TokenType.Number, value));
                    }
                    else
                    {
                        // plain binary operator or unary operator that is not part of a number
                        if (isUnary)
                            tokens.Add(new Token(TokenType.UnaryPlus, 0));
                        else
                            tokens.Add(new Token(TokenType.Plus, 0));
                        i++;
                    }
                    continue;
                }

                // operators * / ^
                if (c == '*' || c == '/' || c == '^')
                {
                    TokenType type = (c == '*') ? TokenType.Multiply :
                                     (c == '/') ? TokenType.Divide : TokenType.Power;
                    tokens.Add(new Token(type, 0, c));
                    i++;
                    continue;
                }

                // any other character is invalid
                throw new FormatException($"Invalid character '{c}'");
            }

            return tokens;
        }

        // ---------- Helper: determine if a sign should be treated as unary ----------
        private static bool IsUnaryPrev(List<Token> tokens)
        {
            if (tokens.Count == 0) return true;

            Token last = tokens[tokens.Count - 1];
            // unary when previous token is '(' or another operator
            return last.Type == TokenType.LeftParen ||
                   last.Type == TokenType.Plus ||
                   last.Type == TokenType.Minus ||
                   last.Type == TokenType.Multiply ||
                   last.Type == TokenType.Divide ||
                   last.Type == TokenType.Power;
        }

        // ---------- Shunting‑Yard algorithm (infix → RPN) ----------
        private static List<object> ShuntingYard(List<Token> tokens)
        {
            Queue<object> output = new Queue<object>();
            Stack<Token> opStack = new Stack<Token>();

            foreach (Token token in tokens)
            {
                if (token.Type == TokenType.Number)
                {
                    output.Enqueue(token);
                }
                else if (token.Type == TokenType.LeftParen)
                {
                    opStack.Push(token);
                }
                else if (token.Type == TokenType.RightParen)
                {
                    while (opStack.Count > 0)
                    {
                        Token top = opStack.Peek();
                        if (top.Type == TokenType.LeftParen)
                        {
                            opStack.Pop();
                            break;
                        }
                        output.Enqueue(top);
                        opStack.Pop();
                    }
                    if (opStack.Count == 0)
                        throw new FormatException("Mismatched parentheses");
                }
                else // operator
                {
                    int prec = GetPrecedence(token.Type);
                    TokenType type = token.Type;

                    while (opStack.Count > 0)
                    {
                        Token top = opStack.Peek();
                        if (top.Type == TokenType.LeftParen) break;

                        int precTop = GetPrecedence(top.Type);
                        bool shouldPop = false;

                        if (precTop > prec)
                        {
                            shouldPop = true;
                        }
                        else if (precTop == prec && IsLeftAssociative(type))
                        {
                            shouldPop = true;
                        }

                        if (shouldPop)
                        {
                            opStack.Pop();
                            output.Enqueue(top);
                        }
                        else
                        {
                            break;
                        }
                    }
                    opStack.Push(token);
                }
            }

            while (opStack.Count > 0)
            {
                Token top = opStack.Pop();
                if (top.Type == TokenType.LeftParen)
                    throw new FormatException("Mismatched parentheses");
                output.Enqueue(top);
            }

            // convert queue to list
            List<object> result = new List<object>();
            while (output.Count > 0)
            {
                result.Add(output.Dequeue());
            }
            return result;
        }

        // ---------- Precedence and associativity ----------
        private static int GetPrecedence(TokenType type)
        {
            switch (type)
            {
                case TokenType.Power: return 5;          // highest
                case TokenType.Multiply:
                case TokenType.Divide: return 4;
                case TokenType.UnaryPlus:
                case TokenType.UnaryMinus: return 3;      // unary binds tighter than * /
                case TokenType.Plus:
                case TokenType.Minus: return 2;
                default: return 0;
            }
        }

        private static bool IsLeftAssociative(TokenType type)
        {
            // '^' and unary operators are right‑associative
            if (type == TokenType.Power ||
                type == TokenType.UnaryPlus ||
                type == TokenType.UnaryMinus)
                return false;
            return true;
        }

        // ---------- Evaluate RPN ----------
        private static double EvaluateRPN(List<object> rpn)
        {
            Stack<double> evalStack = new Stack<double>();

            foreach (object item in rpn)
            {
                if (item is double d)
                {
                    evalStack.Push(d);
                    continue;
                }

                if (item is Token t)
                {
                    switch (t.Type)
                    {
                        case TokenType.Plus:
                            if (evalStack.Count < 1) throw new FormatException("Invalid expression");
                            double b = evalStack.Pop();
                            double a = evalStack.Pop();
                            evalStack.Push(a + b);
                            break;

                        case TokenType.Minus:
                            if (evalStack.Count < 1) throw new FormatException("Invalid expression");
                            double b2 = evalStack.Pop();
                            double a2 = evalStack.Pop();
                            evalStack.Push(a2 - b2);
                            break;

                        case TokenType.Multiply:
                            if (evalStack.Count < 2) throw new FormatException("Invalid expression");
                            double b3 = evalStack.Pop();
                            double a3 = evalStack.Pop();
                            evalStack.Push(a3 * b3);
                            break;

                        case TokenType.Divide:
                            if (evalStack.Count < 2) throw new FormatException("Invalid expression");
                            double b4 = evalStack.Pop();
                            double a4 = evalStack.Pop();
                            if (b4 == 0)
                                throw new DivideByZeroException("Division by zero");
                            evalStack.Push(a4 / b4);
                            break;

                        case TokenType.Power:
                            if (evalStack.Count < 2) throw new FormatException("Invalid expression");
                            double b5 = evalStack.Pop();
                            double a5 = evalStack.Pop();
                            evalStack.Push(Math.Pow(a5, b5));
                            break;

                        case TokenType.UnaryPlus:
                            // just push the value
                            double val = evalStack.Pop();
                            evalStack.Push(val);
                            break;

                        case TokenType.UnaryMinus:
                            if (evalStack.Count < 1) throw new FormatException("Invalid expression");
                            double val2 = evalStack.Pop();
                            evalStack.Push(-val2);
                            break;
                    }
                }
            }

            if (evalStack.Count != 1)
                throw new FormatException("Invalid expression");

            return evalStack.Pop();
        }
    }
}
