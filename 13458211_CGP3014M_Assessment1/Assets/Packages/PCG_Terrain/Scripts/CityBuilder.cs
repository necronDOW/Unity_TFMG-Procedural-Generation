using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityBuilder : MonoBehaviour
{
    public string axiom = "";
    public int simulationCount = 0;

    private Road[] roads;

    private void Start()
    {
        LSystem lsys = new LSystem();
        lsys.AddRule('A', "AB[<BA");
        lsys.AddRule('B', "AA>AB]<");

        roads = Plot(lsys.Simulate(axiom, simulationCount));
    }

    private void OnDrawGizmos()
    {
        if (roads != null)
        {
            for (int i = 0; i < roads.Length; i++)
                Gizmos.DrawLine(roads[i].start, roads[i].end);
        }
    }

    private Road[] Plot(string sentence)
    {
        List<Road> roads = new List<Road>();
        Vector3 origin = transform.position;
        Stack stack = new Stack();

        for (int i = 0; i < sentence.Length; i++)
        {
            switch (sentence[i])
            {
                case 'A': roads.Add(new Road(transform.position, transform.position + transform.forward));
                    transform.position = roads[roads.Count - 1].end;
                    break;
                case 'B': roads.Add(new Road(transform.position, transform.position + transform.forward * 2));
                    transform.position = roads[roads.Count - 1].end;
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

        return roads.ToArray();
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

    struct Road
    {
        public Vector3 start { get; private set; }
        public Vector3 end { get; private set; }

        public Road(Vector3 start, Vector3 end)
        {
            this.start = start;
            this.end = end;
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