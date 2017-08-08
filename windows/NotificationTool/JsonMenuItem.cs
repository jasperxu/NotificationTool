using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotificationTool
{
    class JsonMenuItem
    {
        /// <summary>
        /// 菜单名称
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// 菜单执行命令
        /// </summary>
        public string CMD { get; set; }

        /// <summary>
        /// 菜单前的图标
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// 点击菜单后，通知栏显示图标
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 执行Process时共用
        /// </summary>
        public string CMDKey { get; set; }
    }
}
