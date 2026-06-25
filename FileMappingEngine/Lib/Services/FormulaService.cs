using FileMappingEngine.Lib.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using static FileMappingEngine.Lib.Models.Enums;

namespace FileMappingEngine.Lib.Services
{
    public class FormulaService
    {
        public List<FormulaStep> InterpretFormula(string formula, List<ColumnReference> availableColumns)
        {
            var formulaSteps = new List<FormulaStep>();

            formula = formula.TrimStart('=');

            var tokens = Regex.Matches(
                formula,
                @"\[[^\]]+\]|[+\-*/()]|\d+([.,]\d+)?|[a-zA-Z_]+"
            );

            foreach (Match match in tokens)
            {
                string token = match.Value;

                // Column
                if (token.StartsWith("[") && token.EndsWith("]"))
                {
                    string columnName = token.Trim('[', ']');

                    var column = availableColumns
                        .FirstOrDefault(c => c.Name == columnName);

                    if (column != null)
                    {
                        formulaSteps.Add(new FormulaStep
                        {
                            StepType = FormulaStepType.Column,
                            SelectedColumn = column
                        });
                    }

                    continue;
                }


                // Operator
                if (token == "+" ||
                    token == "-" ||
                    token == "*" ||
                    token == "/")
                {
                    formulaSteps.Add(new FormulaStep
                    {
                        StepType = FormulaStepType.Operator,
                        Operator = token switch
                        {
                            "+" => MathOperator.Add,
                            "-" => MathOperator.Subtract,
                            "*" => MathOperator.Multiply,
                            "/" => MathOperator.Divide,
                            _ => throw new Exception()
                        }
                    });

                    continue;
                }


                // Constant
                if (decimal.TryParse(
                        token.Replace(',', '.'),
                        out _))
                {
                    formulaSteps.Add(new FormulaStep
                    {
                        StepType = FormulaStepType.Constant,
                        Value = token
                    });

                    continue;
                }
            }
            return formulaSteps;
        }
        public string Calculate(DataRow row, List<FormulaStep> steps)
        {
            double result = 0;
            MathOperator? currentOperator = null;
            foreach (var step in steps)
            {
                switch (step.StepType)
                {
                    case FormulaStepType.Column:
                        if (step.SelectedColumn == null)
                            throw new InvalidOperationException("Selected column is null.");
                        string columnName = step.SelectedColumn.Name;
                        if (!row.Table.Columns.Contains(columnName))
                            throw new ArgumentException($"Column '{columnName}' does not exist.");
                        double value = Convert.ToDouble(row[columnName]);
                        result = currentOperator switch
                        {
                            MathOperator.Add => result + value,
                            MathOperator.Subtract => result - value,
                            MathOperator.Multiply => result * value,
                            MathOperator.Divide => result / value,
                            null => value,
                            _ => throw new InvalidOperationException("Unknown operator.")
                        };
                        break;
                    case FormulaStepType.Operator:
                        currentOperator = step.Operator;
                        break;
                    case FormulaStepType.Constant:
                        double constantValue = Convert.ToDouble(step.Value);
                        result = currentOperator switch
                        {
                            MathOperator.Add => result + constantValue,
                            MathOperator.Subtract => result - constantValue,
                            MathOperator.Multiply => result * constantValue,
                            MathOperator.Divide => result / constantValue,
                            null => constantValue,
                            _ => throw new InvalidOperationException("Unknown operator.")
                        };
                        break;
                    default:
                        throw new InvalidOperationException("Unknown formula step type.");
                }
            }
            return result.ToString();
        }
    }
}
