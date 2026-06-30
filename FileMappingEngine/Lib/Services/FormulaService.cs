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
    public class FormulaService
    {
        private List<FormulaToken> _tokens = [];

        private int _position;

        private bool Current(string value)
        {
            if (_position >= _tokens.Count)
                return false;

            return _tokens[_position].Value == value;
        }

        // Tokenize saņem string, un atgriež sarakstu ar FormulaToken objektiem, kas satur informāciju par katru tokenu (tipu un vērtību).
        public static List<FormulaToken> Tokenize(string formula)
        {
            var tokens = new List<FormulaToken>();

            formula = formula.TrimStart('=');

            var matches = Regex.Matches(
                formula,
                @"\[[^\]]+\]|[+\-*/()]|[,;]|\d+([.,]\d+)?|[a-zA-Z_]+"
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


                else if (double.TryParse(value.Replace(',', '.'), CultureInfo.InvariantCulture, out double number))
                {
                    tokens.Add(new FormulaToken
                    {
                        Type = TokenType.Number,
                        Value = number.ToString(CultureInfo.InvariantCulture)
                    });
                }


                else if (value == "+" ||
                         value == "-" ||
                         value == "*" ||
                         value == "/")
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
        // Parse saņem sarakstu ar FormulaToken objektiem un atgriež saknes FormulaNode objektu, kas reprezentē izteiksmes koku.
        public FormulaNode Parse(List<FormulaToken> tokens)
        {
            _tokens = tokens;
            _position = 0;

            return ParseExpression();
        }
        // ParseExpression atgriež FormulaNode objektu, kas reprezentē izteiksmes koku, kurā tiek apstrādāti operatori + un -.
        private FormulaNode ParseExpression()
        {
            var left = ParseTerm();


            while (Current("+") || Current("-"))
            {
                var op = _tokens[_position].Value;

                _position++;

                var right = ParseTerm();

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
        // ParseTerm atgriež FormulaNode objektu, kas reprezentē izteiksmes koku, kurā tiek apstrādāti operatori * un /.
        private FormulaNode ParseTerm()
        {
            FormulaNode left = ParseFactor();


            while (Current("*") || Current("/"))
            {
                string op = _tokens[_position].Value;

                _position++;


                FormulaNode right = ParseFactor();


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
        // ParseFactor atgriež FormulaNode objektu, kas reprezentē izteiksmes koku, kurā tiek apstrādāti skaitļi, kolonnas, funkcijas un iekavas.
        private FormulaNode ParseFactor()
        {
            var token = _tokens[_position];

            // skaitlis
            if (token.Type == TokenType.Number)
            {
                _position++;

                return new FormulaNode
                {
                    Type = FormulaNodeType.Constant,
                    Value = token.Value
                };
            }

            // kolonna [Price]
            if (token.Type == TokenType.Column)
            {
                _position++;

                return new FormulaNode
                {
                    Type = FormulaNodeType.Column,
                    Value = token.Value.Trim('[', ']')
                };
            }

            // iekavas
            if (token.Type == TokenType.OpenParenthesis)
            {
                _position++; // izlaižam '('

                FormulaNode expression = ParseExpression();

                if (_tokens[_position].Type
                    != TokenType.CloseParenthesis)
                {
                    throw new Exception(
                        "Missing closing parenthesis");
                }

                _position++; // izlaižam ')'

                return expression;
            }

            // funkcija
            if (token.Type == TokenType.Function)
            {
                return ParseFunction();
            }

            throw new Exception(
                $"Unexpected token {token.Value}");
        }
        // ParseFunction atgriež FormulaNode objektu, kas reprezentē izteiksmes koku, kurā tiek apstrādātas funkcijas.
        private FormulaNode ParseFunction()
        {
            FormulaToken functionToken = _tokens[_position];

            _position++; // izlaižam funkcijas nosaukumu


            if (!Current("("))
                throw new Exception("Expected '(' after function");

            _position++; // izlaižam '('

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

            while (!Current(")"))
            {
                FormulaNode argument = ParseExpression();

                function.Arguments.Add(argument);

                if (Current(",") || Current(";"))
                {
                    _position++; // izlaižam komatu
                }
                else
                {
                    break;
                }
            }

            if (!Current(")"))
                throw new Exception("Missing ')'");

            _position++; // izlaižam ')'

            return function;
        }
        // Evaluate saņem saknes FormulaNode objektu un DataRow objektu, un atgriež izteiksmes rezultātu kā double.
        public double Evaluate(FormulaNode node, DataRow row)
        {
            return node.Type switch
            {
                FormulaNodeType.Constant => double.Parse(
                                        node.Value!,
                                        CultureInfo.InvariantCulture),
                FormulaNodeType.Column => Convert.ToDouble(
                                        row[node.Value!]),
                FormulaNodeType.Operator => EvaluateOperator(
                                        node,
                                        row),
                FormulaNodeType.Function => EvaluateFunction(
                                        node,
                                        row),
                _ => throw new Exception(
                                        "Unknown node type"),
            };
        }
        // EvaluateOperator saņem FormulaNode objektu, kas reprezentē operatoru, un DataRow objektu, un atgriež izteiksmes rezultātu kā double.
        private double EvaluateOperator(FormulaNode node, DataRow row)
        {
            double left =
                Evaluate(node.Left!, row);

            double right =
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
        // EvaluateFunction saņem FormulaNode objektu, kas reprezentē funkciju, un DataRow objektu, un atgriež izteiksmes rezultātu kā double.
        private double EvaluateFunction(FormulaNode node, DataRow row)
        {
            return node.Function switch
            {
                FormulaFunction.Round => EvaluateRound(node, row),

                _ => throw new InvalidOperationException(
                    $"Unsupported function {node.Function}")
            };
        }
        // EvaluateRound saņem FormulaNode objektu, kas reprezentē ROUND funkciju, un DataRow objektu, un atgriež izteiksmes rezultātu kā double.
        private double EvaluateRound(FormulaNode node, DataRow row)
        {
            if (node.Arguments.Count != 2)
                throw new InvalidOperationException(
                    "ROUND requires 2 arguments.");

            double value = Evaluate(node.Arguments[0], row);

            int decimals = (int)Evaluate(node.Arguments[1], row);

            return Math.Round(value, decimals);
        }
    }
}
