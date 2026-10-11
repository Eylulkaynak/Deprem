from pathlib import Path
p = Path('Assets/LearningApp/Scripts/LearningAppController.cs')
s = p.read_text(encoding='utf-8-sig')
for start, end in [('        private void BuildNavigation()', '        public void ShowTab('), ('        private void ShowEducation()', '        private void Stat('), ('        private void ShowGames()', '        private void OpenGame(')]:
    a, b = s.index(start), s.index(end)
    s = s[:a] + s[b:]
s = s.replace('var titles = Box(header, "grow"); Text(titles, title, "header-title"); Text(titles, subtitle, "header-subtitle");', 'BuildBrandedHeader(title, subtitle);')
# Feedback classes now communicate correctness on the selected answer itself.
s = s.replace('var button = Button(choices, "", () => {', 'Button answerButton = null;\n                var button = answerButton = Button(choices, "", () => {')
s = s.replace('if (answered) return; answered = true; choices.SetEnabled(false); Sound(choice.correct);', 'if (answered) return; answered = true;\n                    answerButton.AddToClassList(choice.correct ? "correct" : "wrong");\n                    choices.SetEnabled(false); Sound(choice.correct);')
s = s.replace('Art(body, finished.icon, "large-art"); Text(body,', 'var reward = Box(body, "reward-emblem"); Art(reward, "icon-star", "reward-star");\n            Text(body, first ? "+30 XP" : "Bilgin güçlendi", "reward-value");\n            Text(body,')
s = s.replace('var card = Box(body, "card"); Text(card, Profile.name, "title");', 'var card = Box(body, "card profile-card"); Art(card, "icon-menu-ranking", "profile-art"); Text(card, Profile.name, "title");')
p.write_text(s, encoding='utf-8')
