using System.Collections;
using System.Reflection;
using System.Text;

namespace TemplateEngine
{
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            if (string.IsNullOrEmpty(htmlTemplate)) return string.Empty;
            return RenderInternal(htmlTemplate, dataModel);
        }

        public string RenderFromFile(string filePath, object dataModel)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл шаблона не найден: {filePath}");

            string template = File.ReadAllText(filePath);
            return RenderFromString(template, dataModel);
        }

        public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
        {
            string result = RenderFromFile(inputFilePath, dataModel);
            File.WriteAllText(outputFilePath, result);
            return result;
        }

        private string RenderInternal(string template, object model)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;

            while (index < template.Length)
            {
                int dollarIndex = template.IndexOf('$', index);
                if (dollarIndex == -1)
                {
                    sb.Append(template.Substring(index));
                    break;
                }

                sb.Append(template.Substring(index, dollarIndex - index));
                index = dollarIndex;

                if (index + 1 < template.Length && template[index + 1] == '{')
                {
                    int closeBrace = template.IndexOf('}', index);
                    if (closeBrace != -1)
                    {
                        string path = template.Substring(index + 2, closeBrace - (index + 2)).Trim();
                        sb.Append(ResolveValue(model, path));
                        index = closeBrace + 1;
                        continue;
                    }
                }

                if (StartsWithKeyword(template, index, "$if"))
                {
                    int blockStart = template.IndexOf('\n', index);
                    if (blockStart == -1) blockStart = index + 3;

                    string conditionPath = template.Substring(index + 3, blockStart - (index + 3)).Trim();
                    var segments = FindIfSegments(template, blockStart);

                    bool condition = EvaluateCondition(model, conditionPath);

                    if (condition)
                    {
                        sb.Append(RenderInternal(segments.IfBody, model));
                    }
                    else if (segments.HasElse)
                    {
                        sb.Append(RenderInternal(segments.ElseBody, model));
                    }

                    index = segments.EndIndex;
                    continue;
                }

                if (StartsWithKeyword(template, index, "$foreach"))
                {
                    int lineEnd = template.IndexOf('\n', index);
                    if (lineEnd == -1) lineEnd = template.Length;

                    string loopHeader = template.Substring(index + 8, lineEnd - (index + 8)).Trim();
                    string[] parts = loopHeader.Split(new[] { " in " }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 2)
                    {
                        string collectionPath = parts[1].Trim();
                        var bodyAndEnd = FindLoopBody(template, lineEnd);

                        IEnumerable collection = ResolveValue(model, collectionPath) as IEnumerable;

                        if (collection != null)
                        {
                            foreach (var item in collection)
                            {
                                sb.Append(RenderInternal(bodyAndEnd.Body, item));
                            }
                        }
                        index = bodyAndEnd.EndIndex;
                        continue;
                    }
                }

                sb.Append('$');
                index++;
            }

            return sb.ToString();
        }

        private object ResolveValue(object model, string path)
        {
            if (model == null || string.IsNullOrEmpty(path)) return string.Empty;

            if (path.Equals("item", StringComparison.OrdinalIgnoreCase))
                return model;

            object? directResult = TryGetPropertyChain(model, path.Split('.'));
            if (directResult != null)
                return directResult;
            int dotIndex = path.IndexOf('.');
            if (dotIndex != -1)
            {
                string withoutPrefix = path.Substring(dotIndex + 1);
                object? prefixedResult = TryGetPropertyChain(model, withoutPrefix.Split('.'));
                if (prefixedResult != null)
                    return prefixedResult;
            }

            return string.Empty;
        }

        private object? TryGetPropertyChain(object root, string[] parts)
        {
            object? currentContext = root;

            foreach (var part in parts)
            {
                if (currentContext == null) return null;

                Type type = currentContext.GetType();
                PropertyInfo? prop = type.GetProperty(
                    part,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop == null) return null;
                currentContext = prop.GetValue(currentContext);
            }

            return currentContext;
        }

        private bool EvaluateCondition(object model, string conditionPath)
        {
            object val = ResolveValue(model, conditionPath);
            if (val is bool b) return b;
            if (val is int i) return i != 0;
            if (val is string s) return !string.IsNullOrEmpty(s);
            return val != null;
        }

        private bool StartsWithKeyword(string text, int index, string keyword)
        {
            if (index + keyword.Length > text.Length) return false;
            return text.Substring(index, keyword.Length) == keyword;
        }

        private (string IfBody, string ElseBody, bool HasElse, int EndIndex) FindIfSegments(string text, int startIndex)
        {
            int nesting = 1;
            int current = startIndex;
            int elseIndex = -1;
            int endifIndex = -1;

            while (current < text.Length)
            {
                if (StartsWithKeyword(text, current, "$if")) { nesting++; current += 3; continue; }
                if (StartsWithKeyword(text, current, "$foreach")) { nesting++; current += 8; continue; }

                if (StartsWithKeyword(text, current, "$else") && nesting == 1)
                {
                    elseIndex = current;
                    current += 5;
                    continue;
                }
                if (StartsWithKeyword(text, current, "$endif") && nesting == 1)
                {
                    endifIndex = current;
                    current += 6;
                    break;
                }

                if (StartsWithKeyword(text, current, "$endif")) { nesting--; current += 6; continue; }
                if (StartsWithKeyword(text, current, "$endfor")) { nesting--; current += 7; continue; }

                current++;
            }

            if (endifIndex == -1) throw new Exception("Пропущено замыкающее выражение $endif");

            if (elseIndex != -1)
            {
                string ifBody = text.Substring(startIndex, elseIndex - startIndex);
                int elseBodyStart = elseIndex + 5;
                if (elseBodyStart < text.Length && text[elseBodyStart] == '\r') elseBodyStart++;
                if (elseBodyStart < text.Length && text[elseBodyStart] == '\n') elseBodyStart++;

                string elseBody = text.Substring(elseBodyStart, endifIndex - elseBodyStart);
                return (ifBody, elseBody, true, endifIndex + 6);
            }
            else
            {
                string ifBody = text.Substring(startIndex, endifIndex - startIndex);
                return (ifBody, string.Empty, false, endifIndex + 6);
            }
        }

        private (string Body, int EndIndex) FindLoopBody(string text, int startIndex)
        {
            int nesting = 1;
            int current = startIndex;
            int endforIndex = -1;

            while (current < text.Length)
            {
                if (StartsWithKeyword(text, current, "$if")) { nesting++; current += 3; continue; }
                if (StartsWithKeyword(text, current, "$foreach")) { nesting++; current += 8; continue; }
                if (StartsWithKeyword(text, current, "$endif")) { nesting--; current += 6; continue; }

                if (StartsWithKeyword(text, current, "$endfor"))
                {
                    nesting--;
                    if (nesting == 0)
                    {
                        endforIndex = current;
                        break;
                    }
                    current += 7;
                    continue;
                }
                current++;
            }

            if (endforIndex == -1) throw new Exception("Пропущено замыкающее выражение $endfor");

            string body = text.Substring(startIndex, endforIndex - startIndex);
            return (body, endforIndex + 7);
        }
    }
}
