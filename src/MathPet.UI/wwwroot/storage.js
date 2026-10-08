window.mathPet = {
    load: () => localStorage.getItem('math-pet-csharp-v1'),
    save: value => localStorage.setItem('math-pet-csharp-v1', value),
    scrollTop: () => window.scrollTo({top: 0, behavior: 'instant'})
};
