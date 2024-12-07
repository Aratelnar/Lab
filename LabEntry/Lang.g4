grammar Lang;

options {
    language=CSharp;
}

//if_stmt returns [cond, value]: 'if' expr '{' func (',' func)* '}' {$cond = $expr.value; $value = $func.value};

prog returns [List<Expression> values]
@init{
    $values = new List<Expression>();
}
: expr5{$values.Add($expr5.value);} (';' expr5{$values.Add($expr5.value);})* EOF;

expr0 returns [Expression value]
: node {$value = $node.value;}
| obj {$value = new ExprObject($obj.result);}
| tuple {$value = new ExprTuple($tuple.result);}
| '#' expr0 {$value = new Template($expr0.value);}
| '(' expr5 ')' {$value = $expr5.value;}
| match {$value = $match.value;}
;

expr1 returns [Expression value]
: expr0 {$value = $expr0.value;}
| f=expr0 '.' w=WORD {$value = new PropertyApplication($f.value, $w.text);}
;

expr2 returns [Expression value]
: expr1 {$value = $expr1.value;}
| f=expr1 a=expr1 {$value = new Application($f.value, $a.value);}
;

expr3 returns [Expression value]
: expr2 {$value = $expr2.value;}
| a=expr2 '&' b=expr2 {$value = new And($a.value, $b.value);}
;

expr4 returns [Expression value]
: expr3 {$value = $expr3.value;}
| a=expr3 '|' b=expr3 {$value = new Or($a.value, $b.value);}
;

expr5 returns [Expression value]
: expr4 {$value = $expr4.value;}
| func {$value = $func.value;}
;

obj returns [Dictionary<string, Expression> result]
@init{
    $result = new Dictionary<string, Expression>();
}
: '{' 
(k=WORD ':' v=expr5 ',' {$result.Add($k.text, $v.value);})*
(k=WORD ':' v=expr5 {$result.Add($k.text, $v.value);})? '}'
;

tuple returns [Dictionary<string, Expression> result]
@init{
    $result = new Dictionary<string, Expression>();
    int i = 0;
}
: '(' 
(k=WORD ':' v=expr5 ',' {$result.Add($k.text, $v.value);})*
(k=WORD ':' v=expr5 {$result.Add($k.text, $v.value);})? ')'
| '('
    (v=expr5 ',' {$result.Add(i.ToString(), $v.value);i++;})*
    (v=expr5 ',' {$result.Add(i.ToString(), $v.value);i++;})
    (v=expr5 {$result.Add(i.ToString(), $v.value);i++;})
')'
;

func returns [Expression value]
: t=expr4 '=>' b=expr4 {$value = new Function($t.value, $b.value);}
;

match returns [Expression value]
@init {
    var result = new List<Expression>();
}
: 'match' arg=expr1 '{' func{result.Add($func.value);} (',' func{result.Add($func.value);})* '}'
{
    $value = new MatchExpr($arg.value, result);
}
;

node returns [Expression value]
@init {
List<Expression> nodes = null;
}
: w=WORD ('(' expr5{nodes ??= new List<Expression>(); nodes.Add($expr5.value);} (',' expr5{nodes.Add($expr5.value);})* ')')?
{
$value = new Node($w.text, nodes);
}
;

WORD:	[a-zA-Z0-9_]+ ;
WHITESPACE: [\t \n\r]+ -> skip;
