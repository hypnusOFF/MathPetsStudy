using MathPet.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
namespace MathPet.UI.Components;

public partial class GameApp
{
    private enum View { Home,Courses,Lesson,Daily,Map,Review,Quiz,Result,Pet,Shop,Bag,Growth }
    private static readonly (View Page,string Icon,string Label)[] Navigation =
    [(View.Home,"⌂","学习首页"),(View.Courses,"▤","学习课堂"),(View.Daily,"✎","每日练习"),
     (View.Map,"⚑","闯关地图"),(View.Review,"↺","错题复习"),(View.Pet,"♡","宠物家园")];
    private static readonly string[] LessonSteps = ["学什么","知识讲解","例题演示","跟着做","独立练习","知识点闯关"];
    private bool loaded;
    private View view;
    private int lessonStep;
    private QuizSession? quiz;
    private SessionSummary? summary;
    private string answerText="";
    private string? answerError;
    private List<string> revealedHints=[];
    private string? toast;
    private CancellationTokenSource? toastCancel;
    private CancellationTokenSource? timerCancel;
    private Task? timerTask;
    private string? modalTitle, modalText, modalButton, modalError;
    private Func<Task>? modalAction;
    private bool modalBusy, renaming;
    private string newPetName="";
    private ElementReference answerInput, modalElement;
    private bool focusAnswer, focusModal;
    private bool disposed;
    private PlayerState State => Game.Engine.State;
    private GameEngine Engine => Game.Engine;
    private string? selectedTopicId;
    private IReadOnlyList<Topic> SemesterTopics => Curriculum.ForSemester(State.Grade, State.Semester);
    private Topic Topic => SemesterTopics.FirstOrDefault(t => t.Id == selectedTopicId) ?? SemesterTopics[0];
    private string GradeName => Curriculum.GradeNames[State.Grade-1];
    private PetPortrait? petPortrait;
    private string VisualStage => State.ActivePet.Stage;
    private string feedbackAction="idle",resultAction="idle";
    private string rewardSpecies="cat",rewardName="团团";
    private PetProgress RewardPet=>State.Pets.Single(p=>p.Species==rewardSpecies);
    private int rewardLevel; private string rewardStage="baby";
    private Task RemoveClothes(string slot)=>Equip(slot, "");
    private void CaptureReward(){rewardSpecies=quiz!.RewardSpecies;rewardName=quiz.RewardPetName;var pet=State.Pets.Single(p=>p.Species==rewardSpecies);rewardLevel=pet.Level;rewardStage=pet.Stage;}
    private async Task SelectPet(string species){Engine.SelectPet(species);await Game.SaveAsync();}
    private async Task BuyClothes(string id){var result=Engine.BuyWearable(id);await Game.SaveAsync();Notify(result.Message);}
    private async Task Equip(string slot,string id){Engine.Equip(slot,id);await Game.SaveAsync();}
    private async Task RestoreDecoration(){State.ActivePet.Accessory="";State.ActivePet.HideStageDecoration=false;await Game.SaveAsync();}
    private async Task SettingsChanged(){await Game.SaveAsync();await JS.InvokeVoidAsync("mathPet.configureSound",State.SoundEnabled);}
    private async Task ChangeRoom(string room){Engine.SelectRoom(room);await Game.SaveAsync();}
    private async Task BuyRoom(string id){var result=Engine.BuyRoom(id);await Game.SaveAsync();Notify(result.Message);}
    private async Task BuyRoomDecoration(string id){var result=Engine.BuyRoomDecoration(id);await Game.SaveAsync();Notify(result.Message);}
    private async Task ToggleRoomDecoration(string id){Engine.PlaceRoomDecoration(id,!State.PlacedRoomDecorations.Contains(id));await Game.SaveAsync();}
    private async Task ClearRoomDecorations(){State.PlacedRoomDecorations.Clear();await Game.SaveAsync();Notify("已收起所有房间装饰。");}
    private async Task RefillTestCoins(){State.Coins=Math.Max(State.Coins,PlayerState.InitialTestCoins);await Game.SaveAsync();Notify("测试积分已补充。");}
    private async Task Comprehensive(string? unit)
    {
        StopTimer();quiz=Engine.StartComprehensive(State.Grade,State.Semester,unit);summary=null;view=View.Quiz;CaptureReward();PrepareQuestion();
        timerCancel=new();timerTask=RunTimer(timerCancel.Token);await JS.InvokeVoidAsync("mathPet.scrollTop");
    }
    private string PageTitle => view switch
    {
        View.Home=>"今天，也进步一点点",View.Courses=>"学习课堂",View.Lesson=>"一起来学新知识",
        View.Daily=>"每日练习",View.Map=>"闯关地图",View.Review=>"错题复习",View.Pet=>"宠物家园",
        View.Shop=>"小岛商店",View.Bag=>"我的背包",View.Growth=>$"{State.PetName}的成长册",
        View.Quiz=>quiz?.Mode==StudyMode.Challenge?"知识点闯关":"专心练习",View.Result=>"本次学习收获",_=>"数学宠物岛"
    };
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(firstRender){await Game.LoadAsync();loaded=true;await Game.SaveAsync();await JS.InvokeVoidAsync("mathPet.configureSound",State.SoundEnabled);StateHasChanged();return;}
        if(focusAnswer){focusAnswer=false;await answerInput.FocusAsync();}
        if(focusModal){focusModal=false;await modalElement.FocusAsync();}
    }
    private bool ActiveNav(View page) => view==page || page==View.Courses&&view==View.Lesson
        || page==View.Pet&&view is View.Shop or View.Bag or View.Growth;
    private static string MasteryLabel(Mastery mastery)=>mastery switch
    {Mastery.NotStarted=>"未学",Mastery.Learning=>"学习中",Mastery.NeedsReview=>"待巩固",_=>"已掌握"};
    private static string StarText(int stars)=>new string('★',stars)+new string('☆',3-stars);
    private async Task ChangeGrade(int grade){State.Grade=grade;selectedTopicId=null;await Game.SaveAsync();}
    private async Task ChangeSemester(Semester semester){State.Semester=semester;selectedTopicId=null;await Game.SaveAsync();}
    private async Task OpenTopic(Topic topic){selectedTopicId=topic.Id;await OpenLesson();}
    private async Task ChallengeTopic(Topic topic){selectedTopicId=topic.Id;await Start(StudyMode.Challenge);}
    private void SelectTopic(ChangeEventArgs e){selectedTopicId=e.Value?.ToString();}
    private async Task OpenLesson(){State.ViewedTopics.Add(Topic.Id);lessonStep=0;await Game.SaveAsync();await Navigate(View.Lesson);}
    private Task Navigate(View next)
    {
        if(view==View.Quiz && quiz is {Completed:false})
        {
            ShowModal("退出本次练习？","已经获得的题目积分会保留，未提交的题目不计分，本次完成奖励不会发放。","退出练习",()=>Go(next));
            return Task.CompletedTask;
        }
        return Go(next);
    }
    private async Task Go(View next)
    {
        StopTimer();view=next;quiz=null;summary=null;
        try{await JS.InvokeVoidAsync("mathPet.scrollTop");}catch(JSException){}
    }
    private async Task Start(StudyMode mode,string? mistakeId=null)
    {
        StopTimer();quiz=mode==StudyMode.Daily ? Engine.StartDaily(State.Grade,State.Semester)
            : Engine.Start(mode,State.Grade,mistakeId,mode==StudyMode.Review ? null : Topic.Id);summary=null;view=View.Quiz;
        CaptureReward();PrepareQuestion();
        if(quiz.IsChallenge)
        {
            timerCancel=new();var token=timerCancel.Token;
            timerTask=RunTimer(token);
        }
        await JS.InvokeVoidAsync("mathPet.scrollTop");
    }
    private async Task RunTimer(CancellationToken token)
    {
        using var ticker=new PeriodicTimer(TimeSpan.FromSeconds(1));
        try{while(await ticker.WaitForNextTickAsync(token))await InvokeAsync(()=>{if(!disposed)StateHasChanged();});}
        catch(OperationCanceledException){}
    }
    private void StopTimer(){timerCancel?.Cancel();timerCancel?.Dispose();timerCancel=null;}
    private void PrepareQuestion(){answerText="";answerError=null;revealedHints=[];focusAnswer=true;feedbackAction="idle";}
    private void Hint(){var hint=quiz?.RevealHint();if(hint is not null)revealedHints.Add($"提示 {quiz!.HintCount}：{hint}");}
    private async Task Submit()
    {
        if(quiz is null||quiz.Answered)return;
        try{var result=quiz.Submit(answerText);feedbackAction=result.Correct?"correct":"encourage";answerError=null;await Game.SaveAsync();await Sound(feedbackAction);}
        catch(ArgumentException ex){answerError=ex.Message;}
    }
    private async Task Next()
    {
        if(quiz is null||!quiz.Answered)return;
        if(quiz.Next()){PrepareQuestion();return;}
        summary=quiz.Finish();var pet=State.Pets.Single(p=>p.Species==rewardSpecies);resultAction=pet.Stage!=rewardStage?"evolve":pet.Level>rewardLevel?"upgrade":summary.Correct>0?"correct":"encourage";view=View.Result;StopTimer();await Game.SaveAsync();await Sound(resultAction);
        await JS.InvokeVoidAsync("mathPet.scrollTop");
    }
    private async Task Feed()
    {
        var food=State.Food; var stage=VisualStage; var text=Engine.Feed();
        await Game.SaveAsync(); Notify(text);
        if(State.Food<food)await Sound("feed");
        if(view==View.Pet && petPortrait is not null && State.Food<food && stage==VisualStage)
            await petPortrait.PlayAsync("feed");
    }
    private async Task Play(){var text=Engine.Play();await Game.SaveAsync();Notify(text);await Sound("play");if(view==View.Pet && petPortrait is not null)await petPortrait.PlayAsync("play");}
    private async Task Care(){var text=Engine.Care();await Game.SaveAsync();Notify(text);await Sound("pet");if(view==View.Pet && petPortrait is not null)await petPortrait.PlayAsync("pet");}
    private async Task Sound(string name){if(State.SoundEnabled)try{await JS.InvokeVoidAsync("mathPet.sound",name);}catch(JSException){}}
    private async Task Buy(ItemKind kind){var result=Engine.Buy(kind);await Game.SaveAsync();Notify(result.Message);}
    private string UseLabel(ItemKind kind)=>kind switch
    {ItemKind.Food=>"喂给"+State.PetName,ItemKind.Toy=>"一起玩",ItemKind.Ribbon=>State.WearingRibbon?"摘下":"戴上",_=>State.Room!="study"?"恢复默认小屋":"布置家园"};
    private async Task Use(ItemKind kind)
    {
        if(!Engine.Owns(kind))return;
        switch(kind)
        {
            case ItemKind.Food:await Feed();return;
            case ItemKind.Toy:await Play();return;
            case ItemKind.Ribbon:State.WearingRibbon=!State.WearingRibbon;Notify(State.WearingRibbon?"已戴上蝴蝶结":"已摘下蝴蝶结");break;
            case ItemKind.Room:State.Room=State.Room=="study"?"spring":"study";State.UsingRoom=State.Room!="study";Notify("家园布置已更新。");break;
        }
        await Game.SaveAsync();
    }
    private void Notify(string message)
    {
        toastCancel?.Cancel();toastCancel?.Dispose();toastCancel=new();toast=message;
        _=ClearToast(toastCancel.Token);
    }
    private async Task ClearToast(CancellationToken token)
    {
        try{await Task.Delay(3000,token);await InvokeAsync(()=>{toast=null;StateHasChanged();});}
        catch(OperationCanceledException){}
    }
    private void ShowModal(string title,string text,string button,Func<Task> action)
    {modalTitle=title;modalText=text;modalButton=button;modalAction=action;modalError=null;modalBusy=false;focusModal=true;}
    private void CancelModal(){modalTitle=null;modalAction=null;renaming=false;}
    private async Task ConfirmModal()
    {
        if(modalBusy||modalAction is null)return;
        modalBusy=true;
        try{await modalAction();if(modalError is null)CancelModal();}
        catch(Exception){modalError="操作未完成，请检查存储空间后重试。";}
        finally{modalBusy=false;}
    }
    private void ModalKey(KeyboardEventArgs e){if(e.Key=="Escape"&&!modalBusy)CancelModal();}
    private void Rename()
    {
        newPetName=State.PetName;renaming=true;
        ShowModal("给伙伴起个名字","名字仅保存在你的设备上。","保存名字",async()=>
        {if(string.IsNullOrWhiteSpace(newPetName)||newPetName.Trim().Length>12){modalError="请输入1～12个字符。";return;}State.PetName=newPetName.Trim();await Game.SaveAsync();modalError=null;Notify("名字已更新。");});
    }
    private void Reset()=>ShowModal("重置本机进度？","积分、宠物和学习记录将恢复初始状态。此操作无法撤销。","确认重置",async()=>{await Game.ResetAsync();await Go(View.Home);Notify("已恢复初始状态。");});
    public async ValueTask DisposeAsync()
    {
        disposed=true;StopTimer();toastCancel?.Cancel();toastCancel?.Dispose();
        if(timerTask is not null)await timerTask;
    }
}
