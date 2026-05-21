namespace MOneClickDownloads.DataModel
{
    /// <summary>
    /// API数据模型的抽象接口。
    /// 所有从 Modrinth API 获取的数据模型都应实现此接口，
    /// 以便统一获取对应的 API 端点路径。
    /// 
    /// 使用场景：
    /// - 调用层可通过此接口获取模型对应的 API 路径，构建请求URL
    /// - 泛型请求方法可使用此约束确保传入的类型具有有效的API端点
    /// </summary>
    public interface IApiModel
    {
        /// <summary>
        /// 获取此数据模型对应的 Modrinth API 端点路径。
        /// 返回值为相对路径（如 "/project/{id|slug}"），需与基础URL拼接使用。
        /// </summary>
        /// <returns>API端点路径模板字符串</returns>
        string GetEndpoint();
    }
}