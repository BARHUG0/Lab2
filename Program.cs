using System.Text;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;

public class Lab2()
{
    static void Main()
    {
        string[] regexes = ReadLines("./exercise1.txt");
        foreach (string regex in regexes)
        {
            //Console.WriteLine(regex);
            //Console.WriteLine(AreGroupingCharactersBalance(regex));
            string postfixRegex = InfixToPostfix(regex);
            ThompsonBuilder builder = new ThompsonBuilder();
            NFA nfa = builder.BuildNFA(postfixRegex);
            GraphExporter.ExportNFA(nfa, "exercise1.svg");
        }


        //string[] ex2 = ReadLines("./exercise2.txt");
        //foreach (string line in ex2)
        //{
        //    Console.WriteLine(AreGroupingCharactersBalance(line));
        //}
    }


    public static string[] ReadLines(string filePath)
    {

        if (!File.Exists(filePath))
        {
            Console.WriteLine("Error: The specified file was not found: " + filePath);
            return Array.Empty<string>();
        }

        string[] lines = File.ReadAllLines(filePath);

        return lines;
    }

    public static bool AreGroupingCharactersBalance(string input)
    {
        Stack<char> stack = new Stack<char>();

        for (int i = 0; i < input.Length; i++)
        {
            char currentChar = input[i];

            //Skip the escaped character and the following one
            if (currentChar == '\\' && i + 1 < input.Length)
            {
                i++;
                continue;
            }

            if (currentChar == '(' || currentChar == '{' || currentChar == '[')
            {
                stack.Push(currentChar);
            }
            else if (currentChar == ')' || currentChar == '}' || currentChar == ']')
            {
                if (stack.Count == 0)
                {
                    return false;
                }

                char top = stack.Pop();

                if ((currentChar == ')' && top != '(') ||
                    (currentChar == '}' && top != '{') ||
                    (currentChar == ']' && top != '['))
                {
                    return false;
                }
            }
        }

        return stack.Count == 0;
    }



    public static int GetPrecedence(char c)
    {
        if (c == '(')
        {
            return 1;
        }
        else if (c == '|')
        {
            return 2;
        }
        else if (c == '&')
        {
            return 3;
        }
        else if (c == '?' || c == '*' || c == '+')
        {
            return 4;
        }
        else if (c == '^')
        {
            return 5;
        }

        return 6;
    }

    public static string FormatRegex(string regex)
    {
        List<char> allOperators = new List<char>() { '|', '?', '+', '*', '^' };
        List<char> binaryOperators = new List<char>() { '^', '|' };

        StringBuilder result = new StringBuilder();

        int i = 0;
        while (i < regex.Length)
        {
            char c1 = regex[i];

            //Handle escaped characters
            if (c1 == '\\' && i + 1 < regex.Length)
            {
                //result.Append(c1); 
                i++;
                result.Append(regex[i]);
            }
            else
            {
                result.Append(c1);
            }

            if (i + 1 < regex.Length)
            {
                char c2 = regex[i + 1];

                bool c1IsNotOpenParen = c1 != '(' && !(c1 == '\\' && i + 1 < regex.Length);
                bool c2IsNotCloseParen = c2 != ')';
                bool c2IsNotOperator = !allOperators.Contains(c2);
                bool c1IsNotBinaryOp = !binaryOperators.Contains(c1);

                if (c1IsNotOpenParen && c2IsNotCloseParen && c2IsNotOperator && c1IsNotBinaryOp)
                {
                    result.Append('&');
                }
            }

            i++;
        }

        return result.ToString();
    }

    public static string InfixToPostfix(string regex)
    {
        StringBuilder postfix = new StringBuilder();
        Stack<char> stack = new Stack<char>();

        string formattedRegex = FormatRegex(regex);

        foreach (char c in formattedRegex)
        {
            switch (c)
            {
                case '(':
                    stack.Push(c);
                    break;

                case ')':
                    while (stack.Count > 0 && stack.Peek() != '(')
                    {
                        postfix.Append(stack.Pop());
                    }
                    if (stack.Count > 0 && stack.Peek() == '(')
                    {
                        stack.Pop();
                    }
                    break;

                default:
                    while (stack.Count > 0)
                    {
                        char peekedChar = stack.Peek();
                        int peekPrecedence = GetPrecedence(peekedChar);
                        int currPrecedence = GetPrecedence(c);

                        if (peekPrecedence >= currPrecedence)
                        {
                            postfix.Append(stack.Pop());
                        }
                        else
                        {
                            break;
                        }
                    }
                    stack.Push(c);
                    break;
            }
        }

        while (stack.Count > 0)
        {
            postfix.Append(stack.Pop());
        }

        return postfix.ToString();
    }

}



public class GraphExporter
{
    public static void ExportNFA(NFA nfa, string fileName)
    {
        Graph graph = new Graph("NFA");

        foreach (State state in nfa.States)
        {
            Node node = graph.AddNode(state.Name);

            if (state == nfa.StartState)
            {
                node.Attr.FillColor = Color.LightGreen;
            }
            if (state == nfa.AcceptState)
            {
                node.Attr.Shape = Shape.DoubleCircle;
                node.Attr.FillColor = Color.LightBlue;
            }
        }

        foreach (Transition transition in nfa.Transitions)
        {
            Edge edge = graph.AddEdge(transition.From.Name, transition.Symbol, transition.To.Name);
            edge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
        }

        string outputFile = fileName.EndsWith(".svg") ? fileName : fileName + ".svg";

        graph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings();

        graph.CreateGeometryGraph();
        graph.GeometryGraph.CalculateLayout();

        using (FileStream stream = new FileStream(outputFile, FileMode.Create))
        {
            SvgGraphWriter writer = new SvgGraphWriter(stream, graph);
            writer.Write();
        }

        Console.WriteLine("Graph exported to: " + outputFile);
    }
}


public class ThompsonBuilder
{
    private int stateCounter = 0;

    private State CreateState()
    {
        string name = "q" + stateCounter;
        stateCounter++;
        return new State(name);
    }

    public NFA BuildNFA(string postfixRegex)
    {
        Stack<NFA> stack = new Stack<NFA>();

        foreach (char token in postfixRegex)
        {
            switch (token)
            {
                case '*':
                    {
                        NFA nfaStar = stack.Pop();
                        NFA result = new NFA();

                        State start = CreateState();
                        State accept = CreateState();

                        result.States.AddRange(nfaStar.States);
                        result.States.Add(start);
                        result.States.Add(accept);
                        result.Transitions.AddRange(nfaStar.Transitions);

                        result.Transitions.Add(new Transition(start, nfaStar.StartState, "eps"));
                        result.Transitions.Add(new Transition(start, accept, "eps"));
                        result.Transitions.Add(new Transition(nfaStar.AcceptState, nfaStar.StartState, "eps"));
                        result.Transitions.Add(new Transition(nfaStar.AcceptState, accept, "eps"));

                        result.StartState = start;
                        result.AcceptState = accept;

                        stack.Push(result);
                        break;
                    }

                case '+':
                    {
                        NFA nfaPlus = stack.Pop();
                        NFA result = new NFA();

                        State start = CreateState();
                        State accept = CreateState();

                        result.States.AddRange(nfaPlus.States);
                        result.States.Add(start);
                        result.States.Add(accept);
                        result.Transitions.AddRange(nfaPlus.Transitions);

                        result.Transitions.Add(new Transition(start, nfaPlus.StartState, "eps"));
                        result.Transitions.Add(new Transition(nfaPlus.AcceptState, nfaPlus.StartState, "eps"));
                        result.Transitions.Add(new Transition(nfaPlus.AcceptState, accept, "eps"));

                        result.StartState = start;
                        result.AcceptState = accept;

                        stack.Push(result);
                        break;
                    }

                case '?':
                    {
                        NFA nfaOptional = stack.Pop();
                        NFA result = new NFA();

                        State start = CreateState();
                        State accept = CreateState();

                        result.States.AddRange(nfaOptional.States);
                        result.States.Add(start);
                        result.States.Add(accept);
                        result.Transitions.AddRange(nfaOptional.Transitions);

                        result.Transitions.Add(new Transition(start, nfaOptional.StartState, "eps"));
                        result.Transitions.Add(new Transition(start, accept, "eps"));
                        result.Transitions.Add(new Transition(nfaOptional.AcceptState, accept, "eps"));

                        result.StartState = start;
                        result.AcceptState = accept;

                        stack.Push(result);
                        break;
                    }

                case '|':
                    {
                        NFA nfa2 = stack.Pop();
                        NFA nfa1 = stack.Pop();
                        NFA result = new NFA();

                        State start = CreateState();
                        State accept = CreateState();

                        result.States.AddRange(nfa1.States);
                        result.States.AddRange(nfa2.States);
                        result.States.Add(start);
                        result.States.Add(accept);
                        result.Transitions.AddRange(nfa1.Transitions);
                        result.Transitions.AddRange(nfa2.Transitions);

                        result.Transitions.Add(new Transition(start, nfa1.StartState, "eps"));
                        result.Transitions.Add(new Transition(start, nfa2.StartState, "eps"));
                        result.Transitions.Add(new Transition(nfa1.AcceptState, accept, "eps"));
                        result.Transitions.Add(new Transition(nfa2.AcceptState, accept, "eps"));

                        result.StartState = start;
                        result.AcceptState = accept;

                        stack.Push(result);
                        break;
                    }

                case '&':
                    {
                        NFA nfa2 = stack.Pop();
                        NFA nfa1 = stack.Pop();
                        NFA result = new NFA();

                        result.States.AddRange(nfa1.States);
                        result.States.AddRange(nfa2.States);
                        result.Transitions.AddRange(nfa1.Transitions);
                        result.Transitions.AddRange(nfa2.Transitions);

                        result.Transitions.Add(new Transition(nfa1.AcceptState, nfa2.StartState, "eps"));

                        result.StartState = nfa1.StartState;
                        result.AcceptState = nfa2.AcceptState;

                        stack.Push(result);
                        break;
                    }

                default:
                    {
                        NFA basic = new NFA();

                        State start = CreateState();
                        State accept = CreateState();

                        basic.States.Add(start);
                        basic.States.Add(accept);
                        basic.Transitions.Add(new Transition(start, accept, token.ToString()));

                        basic.StartState = start;
                        basic.AcceptState = accept;

                        stack.Push(basic);
                        break;
                    }
            }
        }

        return stack.Pop();
    }
}



public class State
{
    public string Name { get; set; }

    public State(string name)
    {
        this.Name = name;
    }
}

public class Transition
{
    public State From { get; set; }
    public State To { get; set; }
    public string Symbol { get; set; }

    public Transition(State fromState, State toState, string symbol)
    {
        this.From = fromState;
        this.To = toState;
        this.Symbol = symbol;
    }
}

public class NFA
{
    public List<State> States { get; set; }
    public List<Transition> Transitions { get; set; }
    public State StartState { get; set; }
    public State AcceptState { get; set; }

    public NFA()
    {
        this.States = new List<State>();
        this.Transitions = new List<Transition>();
    }
}

public class DFA
{
    public List<State> States { get; set; }
    public List<Transition> Transitions { get; set; }
    public State StartState { get; set; }
    public List<State> AcceptStates { get; set; }

    public DFA()
    {
        this.States = new List<State>();
        this.Transitions = new List<Transition>();
        this.AcceptStates = new List<State>();
    }
}


