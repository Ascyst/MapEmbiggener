namespace MapEmbiggener.Extensions
{
    public static class StringExtension
    {
        public static bool ContainsAny(this string str, params string[] substrings)
        {
            foreach (string substr in substrings)
            {
                if (str.Contains(substr))
                {
                    return true;
                }
            }
            return false;
        }
    }

}
