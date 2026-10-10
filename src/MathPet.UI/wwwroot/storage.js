(() => {
 let enabled=true, context;
 const tones={correct:[523,659,784],encourage:[392,440],upgrade:[523,659,784,1047],evolve:[392,523,659,784,1047],feed:[440,554],play:[659,784],pet:[523,659]};
 window.mathPet={
  load:()=>localStorage.getItem('math-pet-csharp-v1'),
  save:value=>{localStorage.setItem('math-pet-csharp-v1',value);try{enabled=JSON.parse(value).SoundEnabled!==false;}catch{}},
  configureSound:value=>{enabled=value;},
  scrollTop:()=>window.scrollTo({top:0,behavior:'instant'}),
  sound:async name=>{if(!enabled)return;try{context??=new(window.AudioContext||window.webkitAudioContext)();await context.resume();(tones[name]||[523]).forEach((f,i)=>{const o=context.createOscillator(),g=context.createGain(),t=context.currentTime+i*.11;o.type='sine';o.frequency.value=f;g.gain.setValueAtTime(0,t);g.gain.linearRampToValueAtTime(.035,t+.01);g.gain.exponentialRampToValueAtTime(.0001,t+.16);o.connect(g);g.connect(context.destination);o.start(t);o.stop(t+.17);});}catch{}}
 };
 try{enabled=JSON.parse(window.mathPet.load()||'{}').SoundEnabled!==false;}catch{}
 document.addEventListener('click',e=>{if(e.target.closest('button')&&enabled)window.mathPet.sound('click');});
})();
