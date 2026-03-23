using System;

namespace third_year_project.Services
{

    //so the general idea here is to have a middle man between running stuff on the ui thread and running stuff on other threads
    //which means we can mock things and test things more easily, and also means we can have a single point of control for how we run things on the ui thread, which is nice
    public interface IAppDispatcher
    {
        void Post(Action callback);
    }
}