window.ytPlayer = (function () {
    let _player = null;
    let _dotNetRef = null;
    let _apiReady = false;
    let _pendingVideoId = null;
    let _pendingElementId = null;

    window.onYouTubeIframeAPIReady = function () {
        _apiReady = true;
        if (_pendingVideoId && _pendingElementId) {
            _createPlayer(_pendingVideoId, _pendingElementId);
            _pendingVideoId = null;
            _pendingElementId = null;
        }
    };

    function _loadApi() {
        if (document.getElementById('yt-iframe-api-script')) return;
        const tag = document.createElement('script');
        tag.id = 'yt-iframe-api-script';
        tag.src = 'https://www.youtube.com/iframe_api';
        document.head.appendChild(tag);
    }

    function _createPlayer(videoId, elementId) {
        if (_player) {
            try { _player.destroy(); } catch (_) {}
            _player = null;
        }
        _player = new YT.Player(elementId, {
            videoId,
            width: '100%',
            height: '100%',
            playerVars: { playsinline: 1, rel: 0, modestbranding: 1 },
            events: {
                onReady: function () {
                    if (_dotNetRef) _dotNetRef.invokeMethodAsync('OnPlayerReady');
                },
                onStateChange: function (e) {
                    if (_dotNetRef) _dotNetRef.invokeMethodAsync('OnPlayerStateChange', e.data);
                }
            }
        });
    }

    return {
        initialize: function (videoId, elementId, dotNetRef) {
            _dotNetRef = dotNetRef;
            _loadApi();
            if (_apiReady) {
                _createPlayer(videoId, elementId);
            } else {
                _pendingVideoId = videoId;
                _pendingElementId = elementId;
            }
        },
        getCurrentTime: function () {
            try { return _player ? _player.getCurrentTime() : 0; } catch (_) { return 0; }
        },
        seekTo: function (seconds) {
            try { if (_player) _player.seekTo(seconds, true); } catch (_) {}
        },
        setPlaybackRate: function (rate) {
            try { if (_player) _player.setPlaybackRate(rate); } catch (_) {}
        },
        destroy: function () {
            try { if (_player) _player.destroy(); } catch (_) {}
            _player = null;
            _dotNetRef = null;
        }
    };
})();
