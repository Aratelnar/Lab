namespace LabEntry.domain;

public abstract record Expression;

public record ExprObject(Dictionary<string, Expression> Properties) : Expression
{
    public override string ToString()
    {
        return $"{{{string.Join(", ", Properties.Select(p => $"{p.Key}: {p.Value}"))}}}";
    }
}

public record ExprTuple(Dictionary<string, Expression> Properties) : Expression
{
    public override string ToString()
    {
        return $"({string.Join(", ", Properties.Select(p => $"{p.Key}: {p.Value}"))})";
    }
}

public record ExprMatch(Expression Argument, List<Expression> Cases) : Expression
{
    public override string ToString()
    {
        return $"Match {Argument} {{{string.Join(", ", Cases.Select(o => o.ToString()))}}}";
    }
}

public record Template(Expression Expression) : Expression
{
    public override string ToString() => $"#{Expression}";
}

public record Function(Expression Template, Expression Body) : Expression
{
    public override string ToString() => $"{Template} => {Body}";
}

public record And(Expression Left, Expression Right) : Expression
{
    public override string ToString() => $"{Left} & {Right}";
}

public record Or(Expression Left, Expression Right) : Expression
{
    public override string ToString() => $"{Left} | {Right}";
}

public record Application(Expression Function, Expression Value) : Expression
{
    public override string ToString() => $"{Function} @ {Value}";
}

public record PropertyApplication(Expression Object, string Property) : Expression
{
    public override string ToString() => $"{Object}.{Property}";
}

// public record Word(string Value) : Expression;

public record Node(string Name, List<Expression> Children) : Expression
{
    public override string ToString() =>
        $"{Name}({(Children is null ? "" : string.Join(", ", Children.Select(o => o.ToString())))})";
}