var dnnBot = dnnBot || {
    showTab: function () {
        $('.bot-tab').removeClass('bot-tab-hide');
    },
    hideTab: function () {
        $('.bot-tab').addClass('bot-tab-hide');
    },
    showChat: function () {
        $('.bot-chat').addClass('bot-chat-show');
    },
    hideChat: function () {
        $('.bot-chat').removeClass('bot-chat-show');
    }
};

jQuery(function () {
    $('.bot-tab').click(function () {
        dnnBot.hideTab();
        setTimeout('dnnBot.showChat()', 200);
    });

    $('.bot-close').click(function () {
        dnnBot.hideChat();
        setTimeout('dnnBot.showTab()', 300);
    });

});