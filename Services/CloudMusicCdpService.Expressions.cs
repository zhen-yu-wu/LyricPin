namespace LyricPin.Services;

public sealed partial class CloudMusicCdpService
{
    private const string BootstrapExpression =
        "(()=>{webpackJsonp.push([[987660],{987660:function(module,exports,require){" +
        "window.__lyricpin_require__=require;}},[[987660]]]);" +
        "const cached=Object.values(window.__lyricpin_require__.c||{});let player=null,storeProvider=null;" +
        "for(const module of cached){const exports=module&&module.exports;if(!exports)continue;" +
        "const values=[exports];if(typeof exports==='object'){for(const key of Object.keys(exports)){try{values.push(exports[key]);}catch{}}}" +
        "for(const value of values){if(!value||typeof value.getStore!=='function')continue;" +
        "try{const state=value.getStore();if(state&&state.playing&&state['async:lyric'])storeProvider=value;}catch{}}" +
        "if(!player){for(const value of values){try{if(value&&typeof value.subscribePlayStatus==='function'){player=value;break;}}catch{}}}}" +
        "if(!player||!storeProvider)return false;window.__lyricpin_store_provider=storeProvider;" +
        "if(!window.__lyricpin_progress_registered){" +
        "window.__lyricpin_progress={current:null,playId:null,updatedAt:0};" +
        "player.subscribePlayStatus({type:'playprogress',callback:e=>{" +
        "window.__lyricpin_progress={current:Number(e.current),playId:e.playId,updatedAt:Date.now()};}});" +
        "player.subscribePlayStatus({type:'seek',callback:e=>{" +
        "window.__lyricpin_progress={current:Number(e.position),playId:e.playId,updatedAt:Date.now()};}});" +
        "window.__lyricpin_progress_registered=true;}return true;})()";

    private const string InstallLyricReaderExpression =
        "(()=>{window.__lyricpin_read_snapshot__=()=>{const state=window.__lyricpin_store_provider.getStore();" +
        "const p=state.playing;const l=state['async:lyric'];" +
        "const trackId=String(p.resourceTrackId||p.onlineResourceId||'');" +
        "const now=Date.now();const trackChanged=window.__lyricpin_lyric_track_id!==trackId;" +
        "if(trackChanged){window.__lyricpin_lyric_track_id=trackId;window.__lyricpin_lyric_requested_at=0;}" +
        "let lines=Array.isArray(l.lyricLines)?l.lyricLines:[];" +
        "if(trackId&&typeof window.__lyricpin_store_provider.getDispatch==='function'&&" +
        "(trackChanged||(!l.isLoading&&lines.length===0&&now-Number(window.__lyricpin_lyric_requested_at||0)>3000))){" +
        "window.__lyricpin_lyric_requested_at=now;try{window.__lyricpin_store_provider.getDispatch()" +
        "({type:'async:lyric/fetchLyric',payload:{force:true}});}catch{}}" +
        "if(trackChanged||now-Number(window.__lyricpin_lyric_requested_at||0)<200)lines=[];" +
        "const localYrc=String(l.yrcInfo?.yrc||'');" +
        "if(trackId&&window.__lyricpin_remote_yrc_track!==trackId){" +
        "window.__lyricpin_remote_yrc_track=trackId;window.__lyricpin_remote_yrc='';" +
        "fetch('https://music.163.com/api/song/lyric?id='+encodeURIComponent(trackId)+'&lv=1&kv=1&tv=-1&yv=1')" +
        ".then(response=>response.ok?response.json():null).then(data=>{" +
        "if(window.__lyricpin_remote_yrc_track===trackId)window.__lyricpin_remote_yrc=String(data?.yrc?.lyric||'');" +
        "}).catch(()=>{});}" +
        "const remoteYrc=window.__lyricpin_remote_yrc_track===trackId?String(window.__lyricpin_remote_yrc||''):'';" +
        "if(lines.length>0){const yrcRaw=remoteYrc.length>=localYrc.length?remoteYrc:localYrc;" +
        "if(window.__lyricpin_yrc_source!==yrcRaw){window.__lyricpin_yrc_source=yrcRaw;" +
        "const parsed=[];const linePattern=/\\[(\\d+),(\\d+)\\]([^\\r\\n]*)/g;let match;" +
        "while((match=linePattern.exec(yrcRaw))!==null){const words=[];" +
        "const wordPattern=/\\((\\d+),(\\d+),\\d+\\)([^\\(]*)/g;let wordMatch;" +
        "while((wordMatch=wordPattern.exec(match[3]))!==null){const text=String(wordMatch[3]||'');words.push({" +
        "start:Number(wordMatch[1])/1000,duration:Number(wordMatch[2])/1000,text,units:Array.from(text).length});}" +
        "const lyric=words.map(word=>word.text).join('');if(lyric){parsed.push({" +
        "time:Number(match[1])/1000,duration:Number(match[2])/1000,lyric,words});}}" +
        "window.__lyricpin_yrc_lines=parsed;}" +
        "const yrcLines=Array.isArray(window.__lyricpin_yrc_lines)?window.__lyricpin_yrc_lines:[];" +
        "if(yrcLines.length>0)lines=yrcLines;}" +
        "const progress=window.__lyricpin_progress||{};" +
        "let position=Number(progress.current);" +
        "if(Number.isFinite(position)&&Number(p.resourceDuration)>0&&position>Number(p.resourceDuration)*10)position/=1000;" +
        "if(progress.playId&&p.playId&&progress.playId!==p.playId)position=NaN;" +
        "let index=-1;if(Number.isFinite(position)){let low=0,high=lines.length-1;" +
        "while(low<=high){const middle=low+((high-low)>>1);if(Number(lines[middle].time)<=position){index=middle;low=middle+1;}else high=middle-1;}}" +
        "const line=index>=0?lines[index]:null;" +
        "const previous=index>0?lines[index-1]:null;" +
        "const next=index+1<lines.length?lines[index+1]:null;" +
        "let lineProgress=0;if(line){const words=Array.isArray(line.words)?line.words:[];" +
        "if(words.length>0&&Number.isFinite(position)){let total=0,completed=0;" +
        "for(const word of words)total+=Number(word.units)||Array.from(word.text).length;" +
        "for(const word of words){const units=Number(word.units)||Array.from(word.text).length;const start=Number(word.start);" +
        "const duration=Math.max(0.001,Number(word.duration));if(position>=start+duration){completed+=units;continue;}" +
        "if(position>start)completed+=units*Math.max(0,Math.min(1,(position-start)/duration));break;}" +
        "lineProgress=total>0?Math.max(0,Math.min(1,completed/total)):0;}else{" +
        "const start=Number(line.time);const end=next?Number(next.time):Number(p.resourceDuration);" +
        "lineProgress=Number.isFinite(end)&&end>start?Math.max(0,Math.min(1,(position-start)/(end-start))):1;}}" +
        "const artists=Array.isArray(p.resourceArtists)?p.resourceArtists.map(a=>a&&a.name).filter(Boolean):[];" +
        "return {id:trackId," +
        "name:String(p.resourceName||''),artist:artists.join(' / ')," +
        "duration:Number(p.resourceDuration||0),playing:Number(p.playingState||0)===2," +
        "index,previousText:previous?String(previous.lyric||''):''," +
        "text:line?String(line.lyric||''):'',nextText:next?String(next.lyric||''):'',lineProgress};};return true;})()";

    private const string LyricExpression = "window.__lyricpin_read_snapshot__()";

    private const string PreviousTrackExpression =
        "(()=>{const dispatch=window.__lyricpin_store_provider?.getDispatch?.();" +
        "if(typeof dispatch!=='function')return false;" +
        "dispatch({type:'playingList/jump2Track',payload:{flag:-1,type:'call'}});return true;})()";

    private const string TogglePlaybackExpression =
        "(()=>{const dispatch=window.__lyricpin_store_provider?.getDispatch?.();" +
        "if(typeof dispatch!=='function')return false;" +
        "dispatch({type:'playing/switchResumeOrPause',payload:{}});return true;})()";

    private const string NextTrackExpression =
        "(()=>{const dispatch=window.__lyricpin_store_provider?.getDispatch?.();" +
        "if(typeof dispatch!=='function')return false;" +
        "dispatch({type:'playingList/jump2Track',payload:{flag:1,type:'call'}});return true;})()";

}
