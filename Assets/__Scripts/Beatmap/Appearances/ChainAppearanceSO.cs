using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Chain Appearance SO", fileName = "ChainAppearanceSO")]
    public class ChainAppearanceSO : ScriptableObject
    {
        public Color RedColor { get; private set; } = BeatSaberSong.DefaultLeftNote;
        public Color BlueColor { get; private set; } = BeatSaberSong.DefaultRightNote;

        public void UpdateColor(Color red, Color blue)
        {
            RedColor = red;
            BlueColor = blue;
        }

        public void SetChainAppearance(ChainContainer chain)
        {
            switch (chain.ChainData.Color)
            {
                case (int)NoteColor.Red:
                    chain.SetColor(RedColor);
                    break;
                case (int)NoteColor.Blue:
                    chain.SetColor(BlueColor);
                    break;
            }

            if (chain.ChainData.CustomColor != null)
                chain.SetColor((Color)chain.ChainData.CustomColor);

            chain.Animator.SetData(chain.ChainData);
        }
    }
}
