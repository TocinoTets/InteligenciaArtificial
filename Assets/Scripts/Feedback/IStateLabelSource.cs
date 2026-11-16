using UnityEngine;

namespace IA.Agents
{
    public interface IStateLabelSource
    {
        string StateLabel { get; }
        Color StateLabelColor { get; }
    }
}