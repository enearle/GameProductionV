using UnityEngine;
using static Sections;

public static class Helpers
{
    public static bool RangeContainsIntExclusive(int value, int a, int b)
    {
        int min = Mathf.Min(a, b);
        int max = Mathf.Max(a, b);
        return value > min && value < max;
    }

    public static bool RangeContainsFloatInclusive(float value, float a, float b)
    {
        float min = Mathf.Min(a, b);
        float max = Mathf.Max(a, b);
        return value >= min && value <= max;
    }
    
    public static float CalculateSectionEntropy(Section section, int zeroPoint, int maxPoint)
    {
        if (maxPoint < zeroPoint)
            return 0;
        
        float sectionXEntropy = (float)(section.size.x - zeroPoint) / (maxPoint - zeroPoint);
        float sectionZEntropy = (float)(section.size.z - zeroPoint) / (maxPoint - zeroPoint);
        sectionXEntropy = Mathf.Clamp(sectionXEntropy, 0f, 1f);
        sectionZEntropy = Mathf.Clamp(sectionZEntropy, 0f, 1f);
        return Mathf.Min(sectionXEntropy, sectionZEntropy);
    }
    
    public static bool LowEntropyRoll(float entropy)
    {
        if (entropy < 0.05f)
        {
            float chance = 1 - entropy * 20;
            return Random.Range(0f, 1f) < chance;
        }
        return false;
    }
    
    public static bool RandomBool()
    {
        return Random.Range(0f, 1f) < 0.5f;
    }
}
