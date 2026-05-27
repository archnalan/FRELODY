// TikTok playback shim — exposes the SAME interface as window.ytPlayer
// (initialize / getCurrentTime / seekTo / destroy + OnPlayerReady /
// OnPlayerStateChange callbacks) so YoutubePlaybackView can drive either player.
//
// TikTok's Player v1 iframe (https://www.tiktok.com/player/v1/{id}) talks over
// postMessage: we send {type, value, 'x-tiktok-player':true} and it posts back
// events incl. periodic onCurrentTime while playing. Because TikTok's state
// codes are inconsistent across versions, we synthesize playing/paused from
// whether time updates keep arriving.
window.tiktokPlayer = (function () {
    let _iframe = null;
    let _dotNetRef = null;
    let _currentTime = 0;
    let _playing = false;
    let _stallTimer = null;
    let _listening = false;

    function _post(type, value) {
        if (!_iframe || !_iframe.contentWindow) return;
        try {
            _iframe.contentWindow.postMessage(
                JSON.stringify({ type, 'x-tiktok-player': true, value }), '*');
        } catch (_) {}
    }

    function _setPlaying(playing) {
        if (playing === _playing) return;
        _playing = playing;
        if (_dotNetRef) _dotNetRef.invokeMethodAsync('OnPlayerStateChange', playing ? 1 : 2);
    }

    function _onMessage(ev) {
        let data = ev.data;
        if (typeof data === 'string') {
            try { data = JSON.parse(data); } catch (_) { return; }
        }
        if (!data || data['x-tiktok-player'] !== true) return;

        switch (data.type) {
            case 'onPlayerReady':
                if (_dotNetRef) _dotNetRef.invokeMethodAsync('OnPlayerReady');
                break;
            case 'onCurrentTime': {
                const v = data.value;
                const t = (v && typeof v === 'object') ? v.currentTime : v;
                if (typeof t === 'number' && !isNaN(t)) {
                    _currentTime = t;
                    _setPlaying(true);
                    clearTimeout(_stallTimer);
                    _stallTimer = setTimeout(() => _setPlaying(false), 600);
                }
                break;
            }
            case 'onStateChange': {
                // Best-effort: treat explicit pause/ended as not playing.
                const v = data.value;
                if (v === 'paused' || v === 'ended' || v === 0 || v === 2 || v === 3) {
                    clearTimeout(_stallTimer);
                    _setPlaying(false);
                }
                break;
            }
        }
    }

    function _buildSrc(videoId) {
        const params = new URLSearchParams({
            autoplay: '0', controls: '1', progress_bar: '1',
            play_button: '1', volume_control: '1', timestamp: '1',
            rel: '0', native_context_menu: '0', closed_caption: '0',
            description: '0', music_info: '0'
        });
        return `https://www.tiktok.com/player/v1/${videoId}?${params.toString()}`;
    }

    return {
        initialize: function (videoId, elementId, dotNetRef) {
            _dotNetRef = dotNetRef;
            _currentTime = 0;
            _playing = false;

            if (!_listening) {
                window.addEventListener('message', _onMessage);
                _listening = true;
            }

            const host = document.getElementById(elementId);
            if (!host) return;
            host.innerHTML = '';

            _iframe = document.createElement('iframe');
            _iframe.src = _buildSrc(videoId);
            _iframe.width = '100%';
            _iframe.height = '100%';
            _iframe.style.border = '0';
            _iframe.allow = 'autoplay; fullscreen; encrypted-media; picture-in-picture';
            _iframe.setAttribute('allowfullscreen', 'true');
            host.appendChild(_iframe);
        },
        getCurrentTime: function () {
            return _currentTime;
        },
        seekTo: function (seconds) {
            _post('seekTo', seconds);
        },
        destroy: function () {
            if (_listening) {
                window.removeEventListener('message', _onMessage);
                _listening = false;
            }
            clearTimeout(_stallTimer);
            if (_iframe && _iframe.parentNode) _iframe.parentNode.removeChild(_iframe);
            _iframe = null;
            _dotNetRef = null;
            _currentTime = 0;
            _playing = false;
        }
    };
})();
