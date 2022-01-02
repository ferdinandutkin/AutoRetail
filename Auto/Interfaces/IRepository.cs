﻿namespace Auto.Interfaces;

public interface IRepository<T>
{
    T Add(T t);
    IEnumerable<T> All();
    void Delete(T t);
    T Update(T t);
}