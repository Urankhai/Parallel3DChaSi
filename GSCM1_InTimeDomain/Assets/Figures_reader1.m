
close all

% exsisting_files = {'168_11', '168_12', '168_22', '171_11', '171_12', '171_21', '171_22'};
% exsisting_files = {'168_11', '168_12', '168_21', '168_22', '171_11', '171_12', '171_21', '171_22'};
exsisting_files = {'171_11', '171_12', '171_21', '171_22'};
colors = {'b','k','g','c','m','y'};
for files_step = 1:length(exsisting_files)
    file_name = ['NarrowPDP', exsisting_files{files_step}, '.fig'];
    disp([ file_name, '; step = ', num2str(mod(files_step-1,4)+1)])
    
    data = openfig(file_name);
%     close figure(1)
    extract = findobj(data,'Type','line');
    
    figure(100)
    grid on
    hold on
    ylim([-115 -65])
    plot(tt2,10*log10(gyy))
    
    plot(tt2,10*log10(gavg).','--r','linewidth',2)
    
    plot(tt2(6:end-5),gsort1,'--k', 'linewidth',1)
    plot(tt2(6:end-5),gsort2,'--k', 'linewidth',1)
    
    for nn = 1:length(extract)
        plot(extract(nn).XData, extract(nn).YData, colors{mod(files_step-1,6)+1},'linewidth', 2);
    end
    
end