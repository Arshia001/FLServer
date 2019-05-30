using Jint;
using Jint.Native;
using Jint.Parser;
using Jint.Parser.Ast;
using Jint.Runtime.References;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FLGrainInterfaces.Util
{
    class RunnableExpressionJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType.GetGenericTypeDefinition() == typeof(RunnableExpression<>);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var typeParam = objectType.GetGenericArguments()[0];
            return Activator.CreateInstance(typeof(RunnableExpression<>).MakeGenericType(typeParam), reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var expField = value.GetType().GetField("expressionString", BindingFlags.NonPublic | BindingFlags.Instance);
            writer.WriteValue(expField.GetValue(value) as string);
        }
    }


    [JsonConverter(typeof(RunnableExpressionJsonConverter))]
    public class RunnableExpression<T>
    {
        readonly string expressionString;
        readonly Expression expression;
        readonly T constantValue;
        readonly bool isConstant;


        public RunnableExpression(string expression)
        {
            expressionString = expression;
            var program = new JavaScriptParser().Parse(expression);
            if (!(program.Body.SingleOrDefault() is ExpressionStatement statement))
                throw new Exception("Expression must contain only a single calculation: " + expression);

            this.expression = statement.Expression;
            if (this.expression.Type == SyntaxNodes.Literal)
            {
                isConstant = true;
                constantValue = ChangeTypeToTarget(JsValue.FromObject(new Engine(), (this.expression as Literal).Value));
            }
        }

        T ChangeTypeToTarget(JsValue value)
        {
            if (value.Type == Jint.Runtime.Types.None || value.Type == Jint.Runtime.Types.Undefined || value.Type == Jint.Runtime.Types.Null)
                return default;

            return JsonConvert.DeserializeObject<T>(value.ToString());
        }

        public T Evaluate(object self, params (string name, object value)[] predefinedObjects)
        {
            if (isConstant)
                return constantValue;

            var engine = new Engine(opt => opt.AllowClrWrite(false).DebugMode(false).LimitRecursion(3).MaxStatements(10).TimeoutInterval(TimeSpan.FromMilliseconds(10)));

            engine.SetValue("Self", self);

            foreach (var (name, value) in predefinedObjects)
                engine.SetValue(name, value);

            var result = engine.EvaluateExpression(expression);
            return ChangeTypeToTarget(engine.GetValue(result));
        }
    }
}
