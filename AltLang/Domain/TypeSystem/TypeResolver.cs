using AltLang.Domain;
using AltLang.Domain.Semantic.Explicit;
using Lang.Util;

namespace Lang.Domain.TypeSystem;

using Assump = Dictionary<string, Term>;
using Subst = Dictionary<WordTemplate, Term>;

public class TypeResolver
{
    private int varCount = 0;
    private Subst substitution = new();

    public Term Subst1(Term infer)
    {
        return ApplySubst(substitution, infer);
    }

    public void Unify(Term left, Term right)
    {
        var s = Substitute(
            ApplySubst(substitution, left),
            ApplySubst(substitution, right)
        ) ?? throw new TypeException();
        substitution = Closure(Intersect(s, substitution))!;
    }

    private Subst? Substitute(Term to, Term from)
    {
        switch (to, from)
        {
            case (var t, WordTemplate v):
                return BindVariable(v, t);
            case (WordTemplate v, var t):
                return BindVariable(v, t);
            case (Or o, var t):
                var left = Substitute(o.Left, t);
                var right = Substitute(o.Right, t);
                return Closure(Union(left, right));
            case (var t, Or o):
                left = Substitute(t, o.Left);
                right = Substitute(t, o.Right);
                return Closure(Intersect(left, right));
            case (AnyTemplate, _):
                return [];
            case (Function fTo, Function fFrom):
                left = Substitute(fFrom.Type, fTo.Type);
                right = Substitute(fTo.Result, fFrom.Result);
                return Closure(Intersect(left, right));
            case (StructureTemplate l, StructureTemplate r) when l.Name == r.Name:
                if (!l.Children.EqualsKeys(r.Children)) return null;
                Subst? s = [];
                foreach (var (_, (ll, rr)) in l.Children.Intersect(r.Children, Tuple.Create))
                {
                    var ss = Substitute(ll, rr);
                    s = Closure(Intersect(ss, s));
                }

                return s;
            default:
                return null;
        }
    }

    private Term Union(Term left, Term right)
    {
        if (left is StructureTemplate {Name: var ln} l && right is StructureTemplate {Name: var rn} r && ln == rn)
        {
            if (l.Children.EqualsKeys(r.Children))
            {
                var dict = new Dictionary<string, Term>();
                foreach (var (key, (ll, rr)) in l.Children.Intersect(r.Children, Tuple.Create))
                {
                    dict[key] = Union(ll, rr);
                }

                return new StructureTemplate(l.Name, dict);
            }
        }

        return new Or(left, right);
    }

    private Subst BindVariable(WordTemplate variable, Term type)
    {
        if (type is WordTemplate {Name: var name} && name == variable.Name)
            return [];
        if (type.TemplateVariables().Contains(variable))
        {
            var n = NewVar();
            var sub = new Subst {[variable] = n};
            var body = ApplySubst(sub, type);
            return new Subst {[variable] = new Rec(n.Name, body)};
        }

        return new Subst {[variable] = type};
    }

    private static Term ApplySubst(Subst subst, Term type) =>
        type switch
        {
            WordTemplate v => subst.GetValueOrDefault(v, v),
            Function abs => new Function(
                ApplySubst(subst, abs.Type),
                ApplySubst(subst, abs.Result)
            ),
            StructureTemplate str => str with
            {
                Children = str.Children.Map(p => ApplySubst(subst, p))
            },
            Rec rec => rec with {Result = ApplySubst(subst, rec.Result)},
            Or or => new Or(
                ApplySubst(subst, or.Left),
                ApplySubst(subst, or.Right)
            ),
            AnyTemplate a => a
        };


    public Subst? Union(Subst? sOver, Subst? s)
    {
        if (sOver is null || s is null) return null;
        return sOver.Over(s, (a, b) => new Or(a, b));
    }

    public Subst? Intersect(Subst? sOver, Subst? s)
    {
        if (sOver is null || s is null) return null;
        return sOver.Over(s, (a, b) => new And(a, b));
    }

    public Subst? Closure(Subst? s)
    {
        if (s is null) return null;
        var result = new Subst(s);
        var keys = s.Keys.ToList();
        foreach (var key in keys)
        {
            var type = result[key];
            if (type.TemplateVariables().Contains(key))
                result[key] = type = BindVariable(key, type)[key];
            foreach (var other in keys)
            {
                if(key == other) continue;
                var otherType = result[other];
                result[other] = ApplySubst(new Subst {[key] = type}, otherType);
            }
        }

        return result;
    }

    public WordTemplate NewVar() => new($"t{varCount++}");
}

public class TypeException : Exception;