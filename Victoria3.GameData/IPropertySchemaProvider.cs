namespace Victoria3.GameData
{
    /// <summary>
    /// プロパティスキーマを提供するためのインターフェース。
    /// プロパティスキーマは、クラスのプロパティの型や名前、値へのアクセス方法を定義するもので、データの構造を動的に扱う際に使用される。
    /// </summary>
    /// <typeparam name="T">プロパティスキーマを提供するクラスの型</typeparam>
    public interface IPropertySchemaProvider<T>
    {
        /// <summary>
        /// クラスのプロパティスキーマの配列を取得する。
        /// </summary>
        public static abstract PropertySchema<T>[] PropertySchemas { get; }
    }
}
