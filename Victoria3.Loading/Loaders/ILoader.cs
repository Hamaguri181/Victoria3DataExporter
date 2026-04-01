namespace Victoria3.Loading.Loaders
{
    /// <summary>
    /// ロード処理を表すインターフェース。
    /// </summary>
    /// <typeparam name="T">ロードされるデータの型。</typeparam>
    public interface ILoader<T>
    {
        /// <summary>
        /// ゲームデータをロードするメソッド。
        /// </summary>
        /// <returns>読み込まれたデータと診断情報を含む <see cref="LoadOutput{T}"/> オブジェクト</returns>
        public LoadOutput<T> Load();
    }
}
