using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPC_AutoMusic.Model
{
    //拟人化“自定义抖动算法”的表达式求值器
    //支持 + - * / % ^、括号、一元正负，以及 abs/sqrt/sin/cos/tan/floor/ceil/round/min/max/pow/clamp/sign
    //可用变量：r(0..1随机)、min、max、beat、count、time、fatigue、strength
    static class HumanizeExpression
    {
        public static bool TryEval(string expression, IDictionary<string, double> variables, out double result)
        {
            result = 0;
            if (String.IsNullOrWhiteSpace(expression))
                return false;
            try
            {
                Parser parser = new Parser(expression, variables);
                result = parser.Parse();
                return !Double.IsNaN(result) && !Double.IsInfinity(result);
            }
            catch
            {
                return false;
            }
        }

        private class Parser
        {
            private readonly string text;
            private readonly IDictionary<string, double> vars;
            private int pos;

            public Parser(string text, IDictionary<string, double> vars)
            {
                this.text = text;
                this.vars = vars;
            }

            public double Parse()
            {
                double value = ParseExpression();
                SkipSpace();
                if (pos < text.Length)
                    throw new FormatException("多余的字符: " + text.Substring(pos));
                return value;
            }

            private double ParseExpression()
            {
                double value = ParseTerm();
                while (true)
                {
                    SkipSpace();
                    if (pos >= text.Length)
                        return value;
                    char c = text[pos];
                    if (c == '+') { pos++; value += ParseTerm(); }
                    else if (c == '-') { pos++; value -= ParseTerm(); }
                    else return value;
                }
            }

            private double ParseTerm()
            {
                double value = ParseFactor();
                while (true)
                {
                    SkipSpace();
                    if (pos >= text.Length)
                        return value;
                    char c = text[pos];
                    if (c == '*') { pos++; value *= ParseFactor(); }
                    else if (c == '/') { pos++; value /= ParseFactor(); }
                    else if (c == '%') { pos++; value %= ParseFactor(); }
                    else return value;
                }
            }

            private double ParseFactor()
            {
                double value = ParseUnary();
                SkipSpace();
                if (pos < text.Length && text[pos] == '^')
                {
                    pos++;
                    //右结合
                    value = Math.Pow(value, ParseFactor());
                }
                return value;
            }

            private double ParseUnary()
            {
                SkipSpace();
                if (pos < text.Length && text[pos] == '-')
                {
                    pos++;
                    return -ParseUnary();
                }
                if (pos < text.Length && text[pos] == '+')
                {
                    pos++;
                    return ParseUnary();
                }
                return ParsePrimary();
            }

            private double ParsePrimary()
            {
                SkipSpace();
                if (pos >= text.Length)
                    throw new FormatException("表达式意外结束");

                char c = text[pos];
                if (c == '(')
                {
                    pos++;
                    double value = ParseExpression();
                    SkipSpace();
                    if (pos >= text.Length || text[pos] != ')')
                        throw new FormatException("缺少右括号");
                    pos++;
                    return value;
                }

                if (Char.IsDigit(c) || c == '.')
                    return ParseNumber();

                if (Char.IsLetter(c) || c == '_')
                    return ParseIdentifier();

                throw new FormatException("无法识别的字符: " + c);
            }

            private double ParseNumber()
            {
                int start = pos;
                while (pos < text.Length && (Char.IsDigit(text[pos]) || text[pos] == '.'))
                    pos++;
                string token = text.Substring(start, pos - start);
                double value;
                if (!Double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    throw new FormatException("数字格式错误: " + token);
                return value;
            }

            private double ParseIdentifier()
            {
                int start = pos;
                while (pos < text.Length && (Char.IsLetterOrDigit(text[pos]) || text[pos] == '_'))
                    pos++;
                string name = text.Substring(start, pos - start);
                SkipSpace();

                //函数调用
                if (pos < text.Length && text[pos] == '(')
                {
                    pos++;
                    List<double> args = new List<double>();
                    SkipSpace();
                    if (pos < text.Length && text[pos] != ')')
                    {
                        args.Add(ParseExpression());
                        SkipSpace();
                        while (pos < text.Length && text[pos] == ',')
                        {
                            pos++;
                            args.Add(ParseExpression());
                            SkipSpace();
                        }
                    }
                    if (pos >= text.Length || text[pos] != ')')
                        throw new FormatException("缺少右括号");
                    pos++;
                    return CallFunction(name, args);
                }

                double value;
                if (vars != null && vars.TryGetValue(name, out value))
                    return value;
                throw new FormatException("未知变量: " + name);
            }

            private static double CallFunction(string name, List<double> a)
            {
                switch (name.ToLowerInvariant())
                {
                    case "abs": return Math.Abs(a[0]);
                    case "sqrt": return Math.Sqrt(a[0]);
                    case "sin": return Math.Sin(a[0]);
                    case "cos": return Math.Cos(a[0]);
                    case "tan": return Math.Tan(a[0]);
                    case "floor": return Math.Floor(a[0]);
                    case "ceil": return Math.Ceiling(a[0]);
                    case "round": return Math.Round(a[0]);
                    case "sign": return Math.Sign(a[0]);
                    case "min": return Math.Min(a[0], a[1]);
                    case "max": return Math.Max(a[0], a[1]);
                    case "pow": return Math.Pow(a[0], a[1]);
                    case "clamp": return Math.Min(Math.Max(a[0], a[1]), a[2]);
                    default: throw new FormatException("未知函数: " + name);
                }
            }

            private void SkipSpace()
            {
                while (pos < text.Length && Char.IsWhiteSpace(text[pos]))
                    pos++;
            }
        }
    }
}
