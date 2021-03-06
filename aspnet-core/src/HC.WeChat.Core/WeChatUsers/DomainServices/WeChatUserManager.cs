﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using HC.WeChat.WeChatUsers;
using HC.WeChat.Shops;
using HC.WeChat.QrCodeLogs;
using HC.WeChat.WechatEnums;
using Abp.Auditing;
using Microsoft.EntityFrameworkCore;

namespace HC.WeChat.WeChatUsers.DomainServices
{
    /// <summary>
    /// WeChatUser领域层的业务管理
    /// </summary>
    public class WeChatUserManager : WeChatDomainServiceBase, IWeChatUserManager
    {
        private readonly IRepository<WeChatUser, Guid> _wechatuserRepository;
        //private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<Shop, Guid> _shopRepository;
        private readonly IRepository<QrCodeLog, Guid> _qrcodelogRepository;

        /// <summary>
        /// WeChatUser的构造方法
        /// </summary>
        public WeChatUserManager(IRepository<WeChatUser, Guid> wechatuserRepository, //IUnitOfWorkManager unitOfWorkManager,
           IRepository<Shop, Guid> shopRepository, IRepository<QrCodeLog, Guid> qrcodelogRepository)
        {
            _wechatuserRepository = wechatuserRepository;
            //_unitOfWorkManager = unitOfWorkManager;
            _shopRepository = shopRepository;
            _qrcodelogRepository = qrcodelogRepository;
        }

        /// <summary>
        /// 绑定微信用户
        /// </summary>
        /// <param name="user"></param>
        public async Task<WeChatUser> BindWeChatUserAsync(WeChatUser user)
        {
            //存在就更新
            if (_wechatuserRepository.GetAll().Any(u => u.Id == user.Id))
            {
                return await _wechatuserRepository.UpdateAsync(user);
            }
            else //新增
            {
                return await _wechatuserRepository.InsertAsync(user);
            }
        }

        /// <summary>
        /// 获取微信用户
        /// </summary>
        public async Task<WeChatUser> GetWeChatUserAsync(string openId, int? tenantId)
        {
            using (CurrentUnitOfWork.SetTenantId(tenantId))
            {
                return await _wechatuserRepository.GetAll().Where(w => w.OpenId == openId).FirstOrDefaultAsync();
            }
        }

        //TODO:编写领域业务代码
        /// <summary>
        ///     初始化
        /// </summary>
        public void InitWeChatUser()
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// 微信关注
        /// </summary>
        [Audited]
        public async Task SubscribeAsync(string openId, string nickName, string headImgUrl, int? tenantId, string scene, string ticket)
        {
            if (string.IsNullOrEmpty(scene))
            {
                await SubscribeNoWithScene(openId, nickName, headImgUrl, tenantId, scene, ticket);
            }
            else
            {
                await SubscribeWithScene(openId, nickName, headImgUrl, tenantId, scene, ticket);
            }
        }

        /// <summary>
        /// 解绑微信用户
        /// </summary>
        //[UnitOfWork]
        //public async Task UnBindWeChatUserAsync(string openId, int? tenantId)
        //{
        //    using (CurrentUnitOfWork.SetTenantId(tenantId))
        //    {
        //        var user = _wechatuserRepository.GetAll().Where(w => w.OpenId == openId).FirstOrDefault(); //await GetWeChatUserAsync(openId, tenantId);
        //        //解绑后变成消费者
        //        if (user != null)
        //        {
        //            user.UserType = WechatEnums.UserTypeEnum.消费者;
        //            user.BindStatus = WechatEnums.BindStatusEnum.未绑定;
        //            user.UserId = null;
        //            user.UserName = user.NickName;
        //            user.UnBindTime = DateTime.Now;
        //            await _wechatuserRepository.UpdateAsync(user);
        //        }
        //    }
        //}

        /// <summary>
        /// 取消关注
        /// </summary>
        [UnitOfWork]
        public async Task UnsubscribeAsync(string openId, int? tenantId)
        {
            var user = await _wechatuserRepository.GetAll().Where(w => w.OpenId == openId).FirstOrDefaultAsync();//await GetWeChatUserAsync(openId, tenantId);
                                                                                                      //解绑后变成消费者
            if (user != null)
            {
                user.UnfollowTime = DateTime.Now;// 取关时间
                user.UserType = WechatEnums.UserTypeEnum.取消关注;
                await _wechatuserRepository.UpdateAsync(user);
            }
        }

        /// <summary>
        /// 带店铺场景值的关注
        /// </summary>
        /// <returns></returns>
        private async Task SubscribeWithScene(string openId, string nickName, string headImgUrl, int? tenantId, string scene, string ticket)
        {
            string[] scenes = new string[0];
            scene = scene.Substring(8);
            scenes = scene.Split("_");

            //Logger.InfoFormat("保存关注场景值id：{0}", scene);
            //Logger.InfoFormat("保存关注场景值数组：{0}", scenes);
            //Logger.InfoFormat("保存关注ticket：{0}", ticket);

            var user = await _wechatuserRepository.GetAll().Where(w => w.OpenId == openId).FirstOrDefaultAsync();//GetWeChatUserAsync(openId, tenantId);

            if (user != null)
            {
                user.NickName = nickName;
                user.UserType = UserTypeEnum.消费者;
                user.UserId = null;
                user.UserName = user.NickName;
                user.BindTime = DateTime.Now;
                user.HeadImgUrl = headImgUrl;
                user.AttentionTime = DateTime.Now; // 第一次关注时间
                if (string.IsNullOrEmpty(user.SourceId))//关注来源
                {
                    user.SourceType = (SceneType)int.Parse(scenes[0]);//关注来源类型
                    user.SourceId = scenes[1];//关注来源Id
                    user.Ticket = ticket;//关注二维码票据
                }
                await _wechatuserRepository.UpdateAsync(user);
            }
            else
            {
                user = new WeChatUser();
                user.NickName = nickName;
                user.OpenId = openId;
                user.TenantId = tenantId;
                user.UserType = UserTypeEnum.消费者;
                user.UserName = nickName;
                user.HeadImgUrl = headImgUrl;
                user.AttentionTime = DateTime.Now; // 最后一次关注时间
                user.IntegralTotal = 0;//积分默认为0
                user.BindStatus = BindStatusEnum.未绑定;
                user.SourceType = (SceneType)int.Parse(scenes[0]);//关注来源类型
                user.SourceId = scenes[1];//关注来源Id
                user.Ticket = ticket;//关注二维码票据
                await _wechatuserRepository.InsertAsync(user);
            }
            SceneType stype = (SceneType)int.Parse(scenes[0]);
            //关注之后新增推广日志
            var qrCodeLog = new QrCodeLog();
            qrCodeLog.AttentionTime = DateTime.Now;
            qrCodeLog.OpenId = openId;
            qrCodeLog.SourceId = scenes[1];
            qrCodeLog.SourceType = stype;
            qrCodeLog.Ticket = ticket;
            await _qrcodelogRepository.InsertAsync(qrCodeLog);

            if (stype == SceneType.店铺)
            {
                Logger.Info("进入店铺粉丝更新");
                var exists = _qrcodelogRepository.GetAll().Any(q => q.SourceId == scenes[1] && q.OpenId == openId);
                if (!exists)
                {
                    var shop = await _shopRepository.GetAsync(new Guid(scenes[1]));
                    shop.FansNum++;
                    Logger.InfoFormat("店铺粉丝数：{0}", shop.FansNum);
                }
            }
            //await CurrentUnitOfWork.SaveChangesAsync();
            //await SaveLogAndFans(openId, scenes[1], int.Parse(scenes[0]), ticket, tenantId);
            ////关注后店铺粉丝统计(方法执行的先后顺序)
            //using (CurrentUnitOfWork.SetTenantId(tenantId))
            //{
            //    //Logger.Info("场景值存在");
            //    //Logger.InfoFormat("场景值类型：{0}", scenes[0]);

            //    ////关注之后新增推广日志
            //    //var qrCodeLog = new QrCodeLog();
            //    //qrCodeLog.AttentionTime = DateTime.Now;
            //    //qrCodeLog.OpenId = openId;
            //    //qrCodeLog.SourceId = scenes[1];
            //    //qrCodeLog.SourceType = (SceneType)int.Parse(scenes[0]);
            //    //qrCodeLog.Ticket = ticket;
            //    //await _qrcodelogRepository.InsertAsync(qrCodeLog);

            //    //if ((SceneType)int.Parse(scenes[0]) == SceneType.店铺)
            //    //{
            //    //    Logger.Info("进入店铺粉丝更新");
            //    //    var exists = _qrcodelogRepository.GetAll().Any(q => q.SourceId == scenes[1] && q.OpenId == user.OpenId);
            //    //    if (!exists)
            //    //    {
            //    //        var shop = await _shopRepository.GetAsync(new Guid(scenes[1]));
            //    //        shop.FansNum++;
            //    //        Logger.InfoFormat("店铺粉丝数：{0}", shop.FansNum);
            //    //    }
            //    //}
            //    //await CurrentUnitOfWork.SaveChangesAsync();
            //    throw new Exception("异常！！！！");
            //}
        }
        /// <summary>
        /// 不带店铺场景值关注
        /// </summary>
        /// <returns></returns>
        private async Task SubscribeNoWithScene(string openId, string nickName, string headImgUrl, int? tenantId, string scene, string ticket)
        {
            var user = await _wechatuserRepository.GetAll().Where(w => w.OpenId == openId).FirstOrDefaultAsync(); //await GetWeChatUserAsync(openId, tenantId);

            if (user != null)
            {
                user.NickName = nickName;
                user.UserType = UserTypeEnum.消费者;
                //user.BindStatus = WechatEnums.BindStatusEnum.未绑定;
                user.UserId = null;
                user.UserName = user.NickName;
                user.BindTime = DateTime.Now;
                user.HeadImgUrl = headImgUrl;
                user.AttentionTime = DateTime.Now; // 第一次关注时间
                await _wechatuserRepository.UpdateAsync(user);
            }
            else
            {
                user = new WeChatUser();
                user.NickName = nickName;
                user.OpenId = openId;
                user.TenantId = tenantId;
                user.UserType = UserTypeEnum.消费者;
                user.UserName = nickName;
                user.HeadImgUrl = headImgUrl;
                user.AttentionTime = DateTime.Now; // 最后一次关注时间
                user.IntegralTotal = 0;//积分默认为0
                user.BindStatus = BindStatusEnum.未绑定;
                await _wechatuserRepository.InsertAsync(user);
            }
        }

        //private async Task SaveLogAndFans(string openId, string sourceId, int type, string ticket, int? tenantId)
        //{
        //    using (CurrentUnitOfWork.SetTenantId(tenantId))
        //    {
        //        //关注之后新增推广日志
        //        var qrCodeLog = new QrCodeLog();
        //        qrCodeLog.AttentionTime = DateTime.Now;
        //        qrCodeLog.OpenId = openId;
        //        qrCodeLog.SourceId = sourceId;
        //        qrCodeLog.SourceType = (SceneType)type;
        //        qrCodeLog.Ticket = ticket;
        //        await _qrcodelogRepository.InsertAsync(qrCodeLog);

        //        if ((SceneType)type == SceneType.店铺)
        //        {
        //            Logger.Info("进入店铺粉丝更新");
        //            var exists = _qrcodelogRepository.GetAll().Any(q => q.SourceId == sourceId && q.OpenId == openId);
        //            if (!exists)
        //            {
        //                var shop = await _shopRepository.GetAsync(new Guid(sourceId));
        //                shop.FansNum++;
        //                Logger.InfoFormat("店铺粉丝数：{0}", shop.FansNum);
        //            }
        //        }
        //        await CurrentUnitOfWork.SaveChangesAsync();
        //    }
        //}
    }
}
