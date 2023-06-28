using System;
using System.Net;
using System.Net.Sockets;

namespace ET.Client
{
    public static partial class LoginHelper
    {
        public static async ETTask Login(Fiber fiber, string account, string password)
        {
            try
            {
                // 创建一个ETModel层的Session
                fiber.RemoveComponent<RouterAddressComponent>();
                // 获取路由跟realmDispatcher地址
                RouterAddressComponent routerAddressComponent = fiber.GetComponent<RouterAddressComponent>();
                if (routerAddressComponent == null)
                {
                    routerAddressComponent = fiber.AddComponent<RouterAddressComponent, string, int>(ConstValue.RouterHttpHost, ConstValue.RouterHttpPort);
                    await routerAddressComponent.Init();
                    
                    fiber.AddComponent<NetClientComponent, AddressFamily>(routerAddressComponent.RouterManagerIPAddress.AddressFamily);
                }
                IPEndPoint realmAddress = routerAddressComponent.GetRealmAddress(account);
                
                R2C_Login r2CLogin;
                using (Session session = await RouterHelper.CreateRouterSession(fiber, realmAddress))
                {
                    r2CLogin = (R2C_Login) await session.Call(new C2R_Login() { Account = account, Password = password });
                }

                // 创建一个gate Session,并且保存到SessionComponent中
                Session gateSession = await RouterHelper.CreateRouterSession(fiber, NetworkHelper.ToIPEndPoint(r2CLogin.Address));
                fiber.AddComponent<SessionComponent>().Session = gateSession;
				
                G2C_LoginGate g2CLoginGate = (G2C_LoginGate)await gateSession.Call(
                    new C2G_LoginGate() { Key = r2CLogin.Key, GateId = r2CLogin.GateId});
                
                fiber.GetComponent<PlayerComponent>().MyId = g2CLoginGate.PlayerId;

                Log.Debug("登陆gate成功!");

                await EventSystem.Instance.PublishAsync(fiber, new EventType.LoginFinish());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        } 
    }
}