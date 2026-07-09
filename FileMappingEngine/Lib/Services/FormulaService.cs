using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Services
{
    public static class FormulaService
    {
        private sealed class ParserState
        {
            public List<FormulaToken> Tokens { get; }
            public int Position;

            public ParserState(List<FormulaToken> tokens) => Tokens = tokens;
        }

        private static bool Current(ParserState state, string value)
        {
            if (state.Position >= state.Tokens.Count)
                return false;

            return state.Tokens[state.Position].Value == value;
        }

        public static List<FormulaToken> Tokenize(string formula)
        {
            var tokens = new List<FormulaToken>();

            formula = formula.TrimStart('=');

            formula = NormalizeDecimals(formula);

            var matches = Regex.Matches(
                formula,
                @"\[[^\]]+\]|[+\-*/()]|[,;]|\d+(?:\.\d+)?|[a-zA-Z_]+"
            );

            foreach (Match match in matches)
            {
                string value = match.Value;

                if (value.StartsWith('['))
                {
                    tokens.Add(new FormulaToken
                    {
                        Type = TokenType.Column,
                        Value = value
                    });
                }

                else if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal number))
                {
                    tokens.Add(new FormulaToken
                    {
                        Type = TokenType.Number,
                        Value = number.ToString(CultureInfo.InvariantCulture)
                    });
                }

                else if (value is "+" or "-" or "*" or "/")
                {
                    tokens.Add(new FormulaToken
                    {
                        Type = TokenType.Operator,
                        Value = value
                    });
                }

                else if (value == "(")
                {
                    tokens.Add(new FormulaToken
                    {
                        Type = TokenType.OpenParenthesis,
                        Value = value
                    });
                }
                else if (value == ")")
                {
                    tokens.Add(new FormulaToken
                    {
                        Type = TokenType.CloseParenthesis,
                        Value = value
                    });
                }

                else if (value == "," || value == ";")
                {
                    tokens.Add(new FormulaToken
                    {
                        Type = TokenType.Comma,
                        Value = value
                    });
                }

                else
                {
                    tokens.Add(new FormulaToken
                    {
                        Type = TokenType.Function,
                        Value = value
                    });
                }
            }

            return tokens;
        }
        private static string NormalizeDecimals(string formula)
        {
            // pārvērš 1,54 → 1.54 (iekšējais formāts vienmēr dot)
            return Regex.Replace(formula, @"(\d),(\d)", "$1.$2");
        }
        public static FormulaNode Parse(List<FormulaToken> tokens)
        {
            var state = new ParserState(tokens);

            return ParseExpression(state);
        }
        private static FormulaNode ParseExpression(ParserState state)
        {
            var left = ParseTerm(state);


            while (Current(state, "+") || Current(state, "-"))
            {
                var op = state.Tokens[state.Position].Value;

                state.Position++;

                var right = ParseTerm(state);

                left = new FormulaNode
                {
                    Type = FormulaNodeType.Operator,
                    Operator = op == "+"
                        ? MathOperator.Add
                        : MathOperator.Subtract,

                    Left = left,
                    Right = right
                };
            }

            return left;
        }
        private static FormulaNode ParseTerm(ParserState state)
        {
            FormulaNode left = ParseFactor(state);


            while (Current(state, "*") || Current(state, "/"))
            {
                string op = state.Tokens[state.Position].Value;

                state.Position++;


                FormulaNode right = ParseFactor(state);


                left = new FormulaNode
                {
                    Type = FormulaNodeType.Operator,

                    Operator = op == "*"
                        ? MathOperator.Multiply
                        : MathOperator.Divide,

                    Left = left,
                    Right = right
                };
            }

            return left;
        }
        private static FormulaNode ParseFactor(ParserState state)
        {
            var token = state.Tokens[state.Position];

            // skaitlis
            if (token.Type == TokenType.Number)
            {
                state.Position++;

                return new FormulaNode
                {
                    Type = FormulaNodeType.Constant,
                    Value = token.Value
                };
            }

            // kolonna [Price]
            if (token.Type == TokenType.Column)
            {
                state.Position++;

                return new FormulaNode
                {
                    Type = FormulaNodeType.Column,
                    Value = token.Value.Trim('[', ']')
                };
            }

            // iekavas
            if (token.Type == TokenType.OpenParenthesis)
            {
                state.Position++; // izlaižam '('

                FormulaNode expression = ParseExpression(state);

                if (state.Tokens[state.Position].Type
                    != TokenType.CloseParenthesis)
                {
                    throw new Exception(
                        "Missing closing parenthesis");
                }

                state.Position++; // izlaižam ')'

                return expression;
            }

            // funkcija
            if (token.Type == TokenType.Function)
            {
                return ParseFunction(state);
            }

            throw new Exception(
                $"Unexpected token {token.Value}");
        }
        private static FormulaNode ParseFunction(ParserState state)
        {
            FormulaToken functionToken = state.Tokens[state.Position];

            state.Position++; // izlaižam funkcijas nosaukumu


            if (!Current(state, "("))
                throw new Exception("Expected '(' after function");

            state.Position++; // izlaižam '('

            var function = new FormulaNode
            {
                Type = FormulaNodeType.Function,
                Function = functionToken.Value.ToUpper() switch
                {
                    "ROUND" => FormulaFunction.Round,

                    _ => throw new Exception(
                        $"Unknown function {functionToken.Value}")
                }
            };

            while (!Current(state, ")"))
            {
                FormulaNode argument = ParseExpression(state);

                function.Arguments.Add(argument);

                if (Current(state, ",") || Current(state, ";"))
                {
                    state.Position++; // izlaižam komatu
                }
                else
                {
                    break;
                }
            }

            if (!Current(state, ")"))
                throw new Exception("Missing ')'");

            state.Position++; // izlaižam ')'

            return function;
        }
        public static decimal Evaluate(FormulaNode node, DataRow row)
        {
            return node.Type switch
            {
                FormulaNodeType.Constant =>
                    decimal.Parse(node.Value!, CultureInfo.InvariantCulture),

                FormulaNodeType.Column =>
                    Convert.ToDecimal(row[node.Value!]),

                FormulaNodeType.Operator =>
                    EvaluateOperator(node, row),

                FormulaNodeType.Function =>
                    EvaluateFunction(node, row),

                _ => throw new Exception("Unknown node type"),
            };
        }
        private static decimal EvaluateOperator(FormulaNode node, DataRow row)
        {
            decimal left =
                Evaluate(node.Left!, row);

            decimal right =
                Evaluate(node.Right!, row);


            return node.Operator switch
            {
                MathOperator.Add
                    => left + right,

                MathOperator.Subtract
                    => left - right,

                MathOperator.Multiply
                    => left * right,

                MathOperator.Divide
                    => left / right,

                _ => throw new Exception()
            };
        }
        private static decimal EvaluateFunction(FormulaNode node, DataRow row)
        {
            return node.Function switch
            {
                FormulaFunction.Round => EvaluateRound(node, row),

                _ => throw new InvalidOperationException(
                    $"Unsupported function {node.Function}")
            };
        }
        private static decimal EvaluateRound(FormulaNode node, DataRow row)
        {
            if (node.Arguments.Count != 2)
                throw new InvalidOperationException("ROUND requires 2 arguments.");

            decimal value = Evaluate(node.Arguments[0], row);

            int decimals = (int)Evaluate(node.Arguments[1], row);

            return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }
    }
}
