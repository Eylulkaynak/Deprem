// Editor authoring only. The shipped game runs native Visual Scripting graphs.
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.VisualScripting;

namespace YanYana.Editor
{
    public sealed class YanYanaGraphAuthor
    {
        public readonly GameObject Owner;
        public readonly ScriptMachine Machine;
        public readonly FlowGraph Graph;
        private int index;

        public YanYanaGraphAuthor(GameObject owner, string title)
        {
            Owner = owner;
            Machine = owner.AddComponent<ScriptMachine>();
            Graph = new FlowGraph { title = title };
            Machine.nest.source = GraphSource.Embed;
            Machine.nest.embed = Graph;
        }

        // Editor migrations can extend a scene's embedded graph without rebuilding it.
        public YanYanaGraphAuthor(ScriptMachine machine)
        {
            Machine = machine;
            Owner = machine.gameObject;
            Graph = machine.graph;
            index = Graph.units.Count;
        }

        public T Add<T>(T unit) where T : Unit
        {
            unit.position = new Vector2((index % 12) * 250, (index / 12) * 210);
            index++;
            Graph.units.Add(unit);
            unit.Define();
            return unit;
        }

        public void Bind(ValueInput input, object value)
        {
            if (value is ValueOutput output)
                Graph.valueConnections.Add(new ValueConnection(output, input));
            else if (input.hasDefaultValue && (value == null || input.type.IsInstanceOfType(value)))
                input.unit.defaultValues[input.key] = value;
            else
            {
                // Object ports and ports without an inline default lose ad-hoc defaults
                // on graph deserialization. A native Literal survives the round trip.
                var literal = Add(new Literal(value?.GetType() ?? input.type, value));
                Graph.valueConnections.Add(new ValueConnection(literal.output, input));
            }
        }

        public void Link(ControlOutput source, ControlInput destination)
        {
            if (source != null && destination != null)
                Graph.controlConnections.Add(new ControlConnection(source, destination));
        }

        public InvokeMember Call(Type type, string name, object target, Type[] parameters, params object[] values)
        {
            var unit = Add(new InvokeMember(new Member(type, name, parameters)));
            if (unit.target != null) Bind(unit.target, target);
            for (int i = 0; i < values.Length; i++)
                if (unit.inputParameters.TryGetValue(i, out ValueInput input)) Bind(input, values[i]);
            return unit;
        }

        public ControlOutput Do(ControlOutput before, Type type, string name, object target, Type[] parameters, params object[] values)
        {
            var unit = Call(type, name, target, parameters, values);
            Link(before, unit.enter);
            return unit.exit;
        }

        public ValueOutput Get(Type type, string name, object target = null)
        {
            var unit = Add(new GetMember(new Member(type, name)));
            if (unit.target != null) Bind(unit.target, target);
            return unit.value;
        }

        public ControlOutput Set(ControlOutput before, Type type, string name, object target, object value)
        {
            var unit = Add(new SetMember(new Member(type, name)));
            if (unit.target != null) Bind(unit.target, target);
            Bind(unit.input, value);
            Link(before, unit.assign);
            return unit.assigned;
        }

        public ValueOutput Var(string name, GameObject owner = null)
        {
            var unit = Add(new GetVariable { kind = VariableKind.Object });
            Bind(unit.name, name);
            Bind(unit.@object, owner ?? Owner);
            return unit.value;
        }

        public ControlOutput SetVar(ControlOutput before, string name, object value, GameObject owner = null)
        {
            var unit = Add(new SetVariable { kind = VariableKind.Object });
            Bind(unit.name, name);
            Bind(unit.@object, owner ?? Owner);
            Bind(unit.input, value);
            Link(before, unit.assign);
            return unit.assigned;
        }

        public void Initial(string name, object value)
        {
            Variables.Object(Owner).Set(name, value);
        }

        public CustomEvent Event(string name, bool coroutine = false, int arguments = 0)
        {
            var unit = new CustomEvent { argumentCount = arguments, coroutine = coroutine };
            Add(unit);
            Bind(unit.name, name);
            Bind(unit.target, Owner);
            return unit;
        }

        public ControlOutput Send(ControlOutput before, object target, string name, params object[] args)
        {
            var unit = Add(new TriggerCustomEvent { argumentCount = args.Length });
            Bind(unit.name, name);
            Bind(unit.target, target);
            for (int i = 0; i < args.Length; i++) Bind(unit.arguments[i], args[i]);
            Link(before, unit.enter);
            return unit.exit;
        }

        public If Branch(ControlOutput before, object condition)
        {
            var unit = Add(new If());
            Bind(unit.condition, condition);
            Link(before, unit.enter);
            return unit;
        }

        public ValueOutput Binary<T>(object a, object b) where T : Unit, new()
        {
            var unit = Add(new T());
            Bind(unit.valueInputs.ElementAt(0), a);
            Bind(unit.valueInputs.ElementAt(1), b);
            return unit.valueOutputs.First();
        }

        public ValueOutput Negate(object value)
        {
            return Call(typeof(bool), "Equals", value, new[] { typeof(bool) }, false).result;
        }

        public ControlOutput Active(ControlOutput before, GameObject target, object value)
        {
            return Do(before, typeof(GameObject), "SetActive", target, new[] { typeof(bool) }, value);
        }

        public ControlOutput Wait(ControlOutput before, float seconds)
        {
            var unit = Add(new WaitForSecondsUnit());
            Bind(unit.valueInputs["seconds"], seconds);
            Bind(unit.valueInputs["unscaledTime"], false);
            Link(before, unit.controlInputs["enter"]);
            return unit.controlOutputs["exit"];
        }

        public void Dirty()
        {
            EditorUtility.SetDirty(Machine);
            EditorUtility.SetDirty(Owner.GetComponent<Variables>());
        }
    }
}
