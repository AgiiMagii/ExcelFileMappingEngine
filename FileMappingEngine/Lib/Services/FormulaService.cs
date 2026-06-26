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
        private List<FormulaToken> _tokens = new();

        private int _position;

        private bool Current(string value)
        {
            if (_position >= _tokens.Count)
                return false;

            return _tokens[_position].Value == value;
        }

        // Tokenize saņem string, un atgriež sarakstu ar FormulaToken objektiem, kas satur informāciju par katru tokenu (tipu un vērtību).
        public List<FormulaToken> Tokenize(string formula)
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


                if (value.StartsWith("["))
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

        public FormulaNode Parse(List<FormulaToken> tokens)
        {
            _tokens = tokens;
            _position = 0;

            return ParseExpression();
        }

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
        public double Evaluate(FormulaNode node, DataRow row)
        {
            switch (node.Type)
            {
                case FormulaNodeType.Constant:

                    return double.Parse(
                        node.Value!,
                        CultureInfo.InvariantCulture);


                case FormulaNodeType.Column:

                    return Convert.ToDouble(
                        row[node.Value!]);


                case FormulaNodeType.Operator:

                    return EvaluateOperator(
                        node,
                        row);


                case FormulaNodeType.Function:

                    return EvaluateFunction(
                        node,
                        row);


                default:
                    throw new Exception(
                        "Unknown node type");
            }
        }
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
        private double EvaluateFunction(FormulaNode node, DataRow row)
        {
            return node.Function switch
            {
                FormulaFunction.Round => EvaluateRound(node, row),

                _ => throw new InvalidOperationException(
                    $"Unsupported function {node.Function}")
            };
        }
        private double EvaluateRound(FormulaNode node, DataRow row)
        {
            if (node.Arguments.Count != 2)
                throw new InvalidOperationException(
                    "ROUND requires 2 arguments.");

            double value = Evaluate(node.Arguments[0], row);

            int decimals = (int)Evaluate(node.Arguments[1], row);

            return Math.Round(value, decimals);
        }
        //public List<FormulaStep> InterpretFormula(string formula, List<ColumnReference> availableColumns)
        //{
        //    var formulaSteps = new List<FormulaStep>();

        //    formula = formula.TrimStart('=');

        //    var tokens = Regex.Matches(
        //        formula,
        //        @"\[[^\]]+\]|[+\-*/()]|\d+([.,]\d+)?|[a-zA-Z_]+"
        //    );

        //    foreach (Match match in tokens)
        //    {
        //        string token = match.Value;

        //        // Column
        //        if (token.StartsWith("[") && token.EndsWith("]"))
        //        {
        //            string columnName = token.Trim('[', ']');

        //            var column = availableColumns
        //                .FirstOrDefault(c => c.Name == columnName);

        //            if (column != null)
        //            {
        //                formulaSteps.Add(new FormulaStep
        //                {
        //                    StepType = FormulaStepType.Column,
        //                    SelectedColumn = column
        //                });
        //            }

        //            continue;
        //        }


        //        // Operator
        //        if (token == "+" ||
        //            token == "-" ||
        //            token == "*" ||
        //            token == "/")
        //        {
        //            formulaSteps.Add(new FormulaStep
        //            {
        //                StepType = FormulaStepType.Operator,
        //                Operator = token switch
        //                {
        //                    "+" => MathOperator.Add,
        //                    "-" => MathOperator.Subtract,
        //                    "*" => MathOperator.Multiply,
        //                    "/" => MathOperator.Divide,
        //                    _ => throw new Exception()
        //                }
        //            });

        //            continue;
        //        }


        //        // Constant
        //        if (double.TryParse(token.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
        //        {
        //            formulaSteps.Add(new FormulaStep
        //            {
        //                StepType = FormulaStepType.Constant,
        //                Value = number.ToString(CultureInfo.InvariantCulture)
        //            });

        //            continue;
        //        }

        //        // Function
        //        if (token.Equals("SUM", StringComparison.OrdinalIgnoreCase) ||
        //            token.Equals("AVERAGE", StringComparison.OrdinalIgnoreCase) ||
        //            token.Equals("MIN", StringComparison.OrdinalIgnoreCase) ||
        //            token.Equals("MAX", StringComparison.OrdinalIgnoreCase) ||
        //            token.Equals("ROUND", StringComparison.OrdinalIgnoreCase))
        //        {
        //            formulaSteps.Add(new FormulaStep
        //            {
        //                StepType = FormulaStepType.Function,
        //                Function = token switch
        //                {
        //                    "SUM" => FormulaFunction.Sum,
        //                    "AVERAGE" => FormulaFunction.Average,
        //                    "MIN" => FormulaFunction.Min,
        //                    "MAX" => FormulaFunction.Max,
        //                    "ROUND" => FormulaFunction.Round,
        //                    _ => throw new Exception()
        //                }
        //            });
        //            continue;
        //        }
        //    }
        //    return formulaSteps;
        //}
        //public string Calculate(DataRow row, List<FormulaStep> steps)
        //{
        //    double result = 0;
        //    MathOperator? currentOperator = null;
        //    foreach (var step in steps)
        //    {
        //        switch (step.StepType)
        //        {
        //            case FormulaStepType.Column:
        //                if (step.SelectedColumn == null)
        //                    throw new InvalidOperationException("Selected column is null.");
        //                string columnName = step.SelectedColumn.Name;
        //                if (!row.Table.Columns.Contains(columnName))
        //                    throw new ArgumentException($"Column '{columnName}' does not exist.");
        //                double value = Convert.ToDouble(row[columnName]);
        //                result = currentOperator switch
        //                {
        //                    MathOperator.Add => result + value,
        //                    MathOperator.Subtract => result - value,
        //                    MathOperator.Multiply => result * value,
        //                    MathOperator.Divide => result / value,
        //                    null => value,
        //                    _ => throw new InvalidOperationException("Unknown operator.")
        //                };
        //                break;
        //            case FormulaStepType.Operator:
        //                currentOperator = step.Operator;
        //                break;
        //            case FormulaStepType.Constant:
        //                double constantValue = Convert.ToDouble(step.Value);
        //                result = currentOperator switch
        //                {
        //                    MathOperator.Add => result + constantValue,
        //                    MathOperator.Subtract => result - constantValue,
        //                    MathOperator.Multiply => result * constantValue,
        //                    MathOperator.Divide => result / constantValue,
        //                    null => constantValue,
        //                    _ => throw new InvalidOperationException("Unknown operator.")
        //                };
        //                break;
        //            case FormulaStepType.Function:
        //                result = ExecuteFunction(step, row);
        //                break;
        //            default:
        //                throw new InvalidOperationException("Unknown formula step type.");
        //        }
        //    }
        //    return result.ToString();
        //}
        //private double ExecuteFunction(FormulaStep function, DataRow row)
        //{
        //    return function.Function switch
        //    {
        //        FormulaFunction.Round => ExecuteRound(function, row),
        //        _ => throw new InvalidOperationException()
        //    };
        //}
        //private double ExecuteRound(FormulaStep function, DataRow row)
        //{
        //    double value =
        //        Convert.ToDouble(Calculate(row, function.Arguments[0]));

        //    int decimals =
        //        Convert.ToInt32(
        //            Calculate(row, function.Arguments[1]));

        //    return Math.Round(value, decimals);
        //}
    }
}
