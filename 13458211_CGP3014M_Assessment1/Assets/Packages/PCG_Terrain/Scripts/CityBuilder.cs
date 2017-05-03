using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityBuilder : MonoBehaviour
{
    Vector2[] plots;

    public int[,] Activate(string axiom, int simulationCount)
    {
        LSystem lsys = new LSystem();
        lsys.AddRule('A', "AB[<BA");
        lsys.AddRule('B', "AA>AB]<");

        Vector2 max, min;
        plots = Plot(lsys.Simulate(axiom, simulationCount), out max, out min);

        Vector2 minAbs = new Vector2(Mathf.Abs(min.x), Mathf.Abs(min.y));
        int[,] grid = new int[(int)(max.x + minAbs.x) + 5, (int)(max.y + minAbs.y) + 5];
        for (int i = 0; i < plots.Length; i++)
            grid[(int)(minAbs.x + plots[i].x) + 2, (int)(minAbs.y + plots[i].y) + 2] = 1;

        return grid;
    }

    private void OnDrawGizmos()
    {
        if (plots != null)
        {
            for (int i = 0; i < plots.Length; i++)
                Gizmos.DrawCube(new Vector3(plots[i].x, 0, plots[i].y), Vector3.one / 10.0f);
        }
    }

    private Vector2[] Plot(string sentence, out Vector2 max, out Vector2 min)
    {
        List<Vector2> plots = new List<Vector2>();
        Vector3 origin = transform.position;
        Stack stack = new Stack();

        for (int i = 0; i < sentence.Length; i++)
        {
            switch (sentence[i])
            {
                case 'A': Forward(transform, plots);
                    break;
                case 'B': Forward(transform, plots);
                    Forward(transform, plots);
                    break;
                case '>': transform.Rotate(0, 90, 0);
                    break;
                case '<': transform.Rotate(0, -90, 0);
                    break;
                case '[': stack.Push(transform.position, transform.rotation);
                    break;
                case ']': stack.Pop(transform);
                    break;
            }
        }

        Evaluate(ref plots, out max, out min);
        return plots.ToArray();
    }

    private void Forward(Transform t, List<Vector2> p)
    {
        t.position += t.forward;
        p.Add(new Vector2(t.position.x, t.position.z));
    }

    private void Evaluate(ref List<Vector2> plots, out Vector2 max, out Vector2 min)
    {
        max = plots[0];
        min = plots[0];

        for (int i = 0; i < plots.Count; i++)
        {
            for (int j = i + 1; j < plots.Count; j++)
            {
                if (plots[i] == plots[j])
                    plots.RemoveAt(j);

                if (plots[i].x > max.x) max.x = plots[i].x;
                else if (plots[i].x < min.x) min.x = plots[i].x;

                if (plots[i].y > max.y) max.y = plots[i].y;
                else if (plots[i].y < min.y) min.y = plots[i].y;
            }
        }
    }

    class Stack
    {
        List<Vector3> points = new List<Vector3>();
        List<Quaternion> rotations = new List<Quaternion>();

        public void Push(Vector3 position, Quaternion rotation)
        {
            points.Add(position);
            rotations.Add(rotation);
        }

        public void Pop(Transform target)
        {
            target.position = points[points.Count - 1];
            points.RemoveAt(points.Count - 1);

            target.rotation = rotations[rotations.Count - 1];
            rotations.RemoveAt(rotations.Count - 1);
        }
    }
}

public class LSystem
{
    private Dictionary<char, string> rules = new Dictionary<char, string>();

    public void AddRule(char a, string b)
    {
        if (!rules.ContainsKey(a))
            rules.Add(a, b);
    }

    public string Simulate(string axiom, int count = 1)
    {
        for (int i = 0; i < axiom.Length; i++)
        {
            if (!rules.ContainsKey(axiom[i]))
                return axiom;
        }

        return SimulateRecursive(count, axiom);
    }

    private string SimulateRecursive(int count, string current)
    {
        if (count == 0)
            return current;

        string newStr = "";

        for (int i = 0; i < current.Length; i++)
            newStr += rules.ContainsKey(current[i]) ? rules[current[i]] : current[i].ToString();

        return SimulateRecursive(count-1, newStr);
    }
}